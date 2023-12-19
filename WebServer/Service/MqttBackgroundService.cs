using MQTTnet.Client;
using MQTTnet;
using System.Text.Json;
using Yolov8Net;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using Models;
using Business.Repository.IRepository;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Business.Repository;
using NuGet.Common;
using DataAccess;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using MQTTnet.Server;

namespace WebServer.Service
{
	public class MqttBackgroundService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;

        /*
		 * Repository 클래스
		 */
        private IEventRepository? _eventRepositroy = null;
		private IBoundingBoxRepository? _boundingBoxRepository = null;
		private IEventVideoRepository? _eventVideoRepository = null;
		private IFCMInfoRepository? _fCMInfoRepository = null;

		/*
		 * 이벤트가 발생하였을 때 한번 더 Object Detection하고
		 * 이벤트 영상을 만들기 위한 Mqtt 클라이언트
		 */
		private IMqttClient? MqttClient { get; set; } = null;

        /*
		 * 단일 이벤트에 대한 이미지가 저장되었을 때 해당 이벤트 ID를 카메라에게 전달하는 Mqtt 클라이언트
		 * 카메라는 받은 이벤트 ID 중 하나의 영상으로 만들어야 하는 이벤트 ID 리스트를 event/video/create 토픽으로 전송함
		 */
        private IMqttClient? EventSender { get; set; } = null;

		private static readonly string ROOT = @"/home/shyoun/Desktop/GraduationWorks/WebServer/wwwroot";
		//private static readonly string ROOT = @"C:\Users\hisn16.DESKTOP-HGVGADP\source\repos\GraduationWorks\WebServer\wwwroot\";
		private static readonly string FCM_SERVER_KEY = "AAAAlAPqkMU:APA91bEpsixt1iwXs5ymw67EvF8urDy9Mi3gVbLEYYlgAit94zctOhQuO12pvsD2tuk5oJtzZ9eGAwblxebKyBM8WEQDhYm2ihhBuud5P7cESyFfAycI--IhY4jJ4m2Yr-lJ27qSGK7w";

		public MqttBackgroundService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			using (var scope = _serviceProvider.CreateScope())
			{
                /*
				 * Repositroy 클래스는 의존성 컨테이너에 등록되어 있음
				 * ServiceProvider를 통하여 현재 스코프에서 Repositroy 클래스 객체 주입
				 */
                _eventRepositroy = scope.ServiceProvider.GetRequiredService<IEventRepository>();
				_boundingBoxRepository = scope.ServiceProvider.GetRequiredService<IBoundingBoxRepository>();
				_eventVideoRepository = scope.ServiceProvider.GetRequiredService<IEventVideoRepository>();
				_fCMInfoRepository = scope.ServiceProvider.GetRequiredService<IFCMInfoRepository>();

				/*
				 * Object Detection 결과 이미지에서 사용하는 폰트
				 */
				var font = new FontCollection().Add($"{ROOT}/CONSOLA.TTF").CreateFont(11, FontStyle.Bold);

				/*
				 * YOLOv8 모델 객체
				 */
				using var coco = YoloV8Predictor.Create($"{ROOT}/models/coco.onnx", useCuda: true);
				using var fire = YoloV8Predictor.Create($"{ROOT}/models/fire.onnx", labels: new string[] { "fire", "smoke" }, useCuda: true);

                //using var coco = YoloV8Predictor.Create($"{ROOT}/models/coco.onnx");
                //using var fire = YoloV8Predictor.Create($"{ROOT}/models/fire.onnx", labels: new string[] { "fire", "smoke" });

                /*
				 * event 토픽에서 카메라로부터 수신한 이벤트를 한번 더 Object Detection
				 * event/video/create 토픽에서 이벤트 영상을 만듬
				 */
                var mqttFactory = new MqttFactory();
				EventSender = mqttFactory.CreateMqttClient();
				var options = new MqttClientOptionsBuilder()
					.WithTcpServer("ictrobot.hknu.ac.kr", 8085)
					.Build();
				await EventSender.ConnectAsync(options, CancellationToken.None);

				MqttClient = mqttFactory.CreateMqttClient();
				MqttClient.ApplicationMessageReceivedAsync += async e =>
				{
					try
					{
						var message = e.ApplicationMessage;

                        // 카메라로부터 수신한 이벤트를 한번 더 Object Detection하는 부분
                        if (message.Topic == "event")
						{
							var payload = e.ApplicationMessage.PayloadSegment;

							// 카메라에서 Object Detection한 결과
							var request = JsonSerializer.Deserialize<DetectionDTO>(payload);
							if (request == null || request.Image == null)
							{
								return;
							}
                            // Base64 디코딩
                            var image = Convert.FromBase64String(request.Image.Replace("data:image/jpeg;base64,", string.Empty));
							string inputImagePath = string.Empty;
							using (var stream = new MemoryStream(image))
							{
								inputImagePath = Path.Combine(ROOT, "images", $"{Guid.NewGuid()}.jpeg");
								using (var fileStream = new FileStream(inputImagePath, FileMode.Create))
								{
									await stream.CopyToAsync(fileStream);
								}
							}

							IPredictor model = null;
							switch (request.Model)
							{
								case "coco":
									model = coco;
									break;
								case "fire":
									model = fire;
									break;
							}

							using var input = Image.Load(inputImagePath);
							if (model == null)
							{
								throw new ArgumentNullException(nameof(model));
							}
							if (input == null)
							{
								throw new ArgumentNullException(nameof(input));
							}

							var predictions = model.Predict(input);
							// Object Detection 실패한 경우
							if (!predictions.Any())
							{
								await Task.CompletedTask;
							}

                            // Class Score가 0.5 이상인 Bounding Box를 이미지에 그리는 부분
                            var boundingBoxes = new List<BoundingBoxDTO>();
							foreach (var prediction in predictions)
							{
								if (prediction.Score < 0.5) continue;

								var originalImageHeight = input.Height;
								var originalImageWidth = input.Width;

								var x = (int)Math.Max(prediction.Rectangle.X, 0);
								var y = (int)Math.Max(prediction.Rectangle.Y, 0);
								var width = (int)Math.Min(originalImageWidth - x, prediction.Rectangle.Width);
								var height = (int)Math.Min(originalImageHeight - y, prediction.Rectangle.Height);
								
								var text = $"{prediction.Label.Name}: {prediction.Score}";
								var size = TextMeasurer.Measure(text, new TextOptions(font));
								
								input.Mutate(d => d.Draw(Pens.Solid(Color.Yellow, 2), new Rectangle(x, y, width, height)));
								input.Mutate(d => d.DrawText(new TextOptions(font) { Origin = new Point(x, (int)(y - size.Height - 1)) }, text, Color.Yellow));

                                // Class Score가 0.5 이상인 Bounding Box 저장
                                var boundingBox = new BoundingBoxDTO
								{
									X = x,
									Y = y,
									Width = width,
									Height = height,
									Label = prediction.Label.Name,
									Confidence = prediction.Score,
								};
								boundingBoxes.Add(boundingBox);
							}

							// 이벤트 이미지 저장
							var eventImagePath = Path.Combine(ROOT, "images", $"{Guid.NewGuid()}.jpeg");
							input.Save(eventImagePath);

                            // 해당 이벤트 정보
                            // EventVideoId 필드는 현재 이벤트가 영상으로 저장되었을 때 그 이벤트 영상 ID가 할당됨
                            var eventDTO = new EventDTO
							{
								Date = request.Date,
								CameraId = request.CameraId,
								Path = eventImagePath,
								EventVideoId = null,
							};

                            // Event 테이블에 eventDTO의 데이터 저장
                            var createdEventDTO = await _eventRepositroy.Create(eventDTO);

                            // 데이터베이스에 저장한 이벤트의 ID를 카메라에게 전송하는 부분
                            // 카메라는 수신한 이벤트 ID 중 하나의 영상으로 만들어야 하는 이벤트 ID 리스트를 event/video/create 토픽으로 전송
                            var createdEvent = JsonSerializer.Serialize<CreatedEventDTO>(new CreatedEventDTO
							{
								Id = createdEventDTO.Id,
								CameraId = createdEventDTO.CameraId
							});
							var applicationMessage = new MqttApplicationMessageBuilder()
								.WithTopic("event/create")
								.WithPayload(createdEvent)
								.Build();
							await EventSender.PublishAsync(applicationMessage, CancellationToken.None);

                            // BoundingBox 테이블에 BoundingBoxDTO의 데이터 저장
                            foreach (var boundingBox in boundingBoxes)
							{
                                // Event 테이블에서 이벤트 객체를 쿼리할 때 해당 이벤트의 Bounding Box도 가져올 수 있도록
                                // Event 객체와 Bounding Box 객체 바인딩
                                boundingBox.EventId = createdEventDTO.Id;
							}
							var count = await _boundingBoxRepository.Create(boundingBoxes);
							if (count <= 0)
							{
								throw new Exception($"BoundingBox를 저장하는데 실패했습니다.");
							}

							// 입력 이미지 삭제
							if (File.Exists(inputImagePath))
							{
								File.Delete(inputImagePath);
							}
						}

						// 이벤트 영상을 만드는 부분
						else if (message.Topic == "event/video/create")
						{
							var payload = e.ApplicationMessage.PayloadSegment;

							// 카메라에게 하나의 영상으로 만들어야 하는 이벤트 ID 리스트 수신
							var request = JsonSerializer.Deserialize<CreateEventVideoDTO>(payload);
							if (request == null)
							{
								return;
							}

							// 이벤트 ID 리스트에 포함된 이벤트 쿼리
							var eventDTOs = (await _eventRepositroy.GetAll(request.EventIds)).ToList();
							if (!eventDTOs.Any())
							{
								return;
							}

                            // 쿼리한 이벤트의 이미지가 저장된 경로를 imagePaths에 저장하는 부분
                            var imagePaths = new List<string>();
							foreach (var eventDTO in eventDTOs)
							{
								if (!string.IsNullOrEmpty(eventDTO.Path))
								{
									imagePaths.Add(eventDTO.Path);
								}
							}

							// 이벤트 영상을 만드는 부분
							if (imagePaths.Any())
							{
								// 영상으로 만들어야 하는 순서대로 이미지 사본 생성 및 이미지 파일 이름 변경
								var identifier = Guid.NewGuid().ToString();
								for (int i = 0; i < imagePaths.Count; i++)
								{
									var destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{identifier}_{i + 1}.jpeg");
									File.Copy(imagePaths[i], destinationPath);
								}

                                // 이벤트 영상 경로
                                var videoPath = $"{Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos", $"{Guid.NewGuid()}")}.mp4";
                                // FFmpeg 파라미터
                                var args = $"-framerate 1 -i {Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", identifier)}_%d.jpeg -c:v libx264 -r 30 -pix_fmt yuv420p {videoPath}";
								// FFmpeg 프로그램 실행
								var ffMpeg = new Process
								{
									StartInfo = new ProcessStartInfo
									{
										FileName = "ffmpeg",
										Arguments = args,
										UseShellExecute = false,
										RedirectStandardOutput = true,
										CreateNoWindow = false,
										RedirectStandardError = true
									},
									EnableRaisingEvents = true
								};
								ffMpeg.Start();

								// 진행상황 로깅
								var processOutput = string.Empty;
								while ((processOutput = ffMpeg.StandardError.ReadLine()) != null)
								{
									Console.WriteLine(processOutput);
								}

								// 저장한 이벤트 영상의 정보를 EventVideo 테이블에 저장
								var createdVideoDTO = await _eventVideoRepository.Create(new EventVideoDTO
								{
									UserId = request.UserId,
									Path = videoPath
								});

								var labels = new HashSet<string>();
								foreach (var eventDTO in eventDTOs)
								{
									// 이벤트 영상과 영상으로 저장한 이벤트를 바인딩하는 부분
									// 하나의 EventVideo 객체는 다수의 Event 객체와 바인딩됨
									eventDTO.EventVideoId = createdVideoDTO.Id;
									await _eventRepositroy.Update(eventDTO);

                                    // FCM 알람에 무엇을 탐지했는지 보여주기 위해 탐지한 레이블 리스트를 문자열로 바꿈
                                    // ex. person, fire이 탐지된 경우, "person, fire" 문자열을 만듬
                                    foreach (var label in eventDTO.BoundingBoxes.Select(x => x.Label).Distinct())
									{
										if (string.IsNullOrEmpty(label)) continue;
										labels.Add(label);
									}
								}
								var labels_string = string.Join(", ", labels);

								// FCM 알람을 보낼 유저를 ID를 통하여 쿼리
								// 쿼리한 FCMInfo 객체에는 클라이언트 디바이스의 FCM 토큰에 해당하는 Token 필드가 있음
								var fcmInfos = await _fCMInfoRepository.GetAllByUserId(request.UserId);
								if (fcmInfos.Any())
								{
									var _title = string.Empty;
									if (labels.Any())
									{
										// 알람 제목으로 탐지된 객체의 문자열 지정
										_title = labels_string;
									}

									foreach (var fcmInfo in fcmInfos)
									{
										var content = new
										{
											to = fcmInfo.Token,
											data = new
											{
												title = _title,
												body = new
												{
													cameraId = request.CameraId,
													description = $"{request.CameraId}번 카메라에서 {_title} 이벤트가 발생하였습니다."
												}
											}
										};

										// FCM 알람 전송
										using (var client = new HttpClient())
										{
											client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
											client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={FCM_SERVER_KEY}");
											var response = await client.PostAsJsonAsync(@"https://fcm.googleapis.com/fcm/send", content);
											Console.WriteLine(await response.Content.ReadAsStringAsync());
										}
									}
								}

								// 이벤트 이미지 사본 삭제
								for (int i = 0; i < imagePaths.Count; i++)
								{
									var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{identifier}_{i + 1}.png");
									if (File.Exists(filePath))
									{
										File.Delete(filePath);
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.StackTrace);
						Console.WriteLine(ex.Message);
					}
				};

				var mqttClientOptions = new MqttClientOptionsBuilder()
					.WithTcpServer("ictrobot.hknu.ac.kr", 8085)
					.Build();
				await MqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

				var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
					.WithTopicFilter(f => { f.WithTopic("event"); })
					.WithTopicFilter(f => { f.WithTopic("event/video/create"); })
					.Build();
				await MqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

				// 백그라운드 서비스가 종료되지 않도록 함
				while (!stoppingToken.IsCancellationRequested)
				{
					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
			}
		}
	}
}
