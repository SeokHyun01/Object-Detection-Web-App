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

namespace WebServer.Service
{
	public class MqttBackgroundService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private IEventRepository? _eventRepositroy = null;
		private IBoundingBoxRepository? _boundingBoxRepository = null;
		private IEventVideoRepository? _eventVideoRepository = null;
		private IMqttClient? MqttClient { get; set; } = null;

		private static readonly string ROOT = @"/home/shyoun/Desktop/GraduationWorks/WebServer/wwwroot";
		private static readonly Font FONT = new FontCollection().Add($"{ROOT}/CONSOLA.TTF").CreateFont(11, FontStyle.Bold);

		public MqttBackgroundService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			using (var scope = _serviceProvider.CreateScope())
			{
				_eventRepositroy = scope.ServiceProvider.GetRequiredService<IEventRepository>();
				_boundingBoxRepository = scope.ServiceProvider.GetRequiredService<IBoundingBoxRepository>();
				_eventVideoRepository = scope.ServiceProvider.GetRequiredService<IEventVideoRepository>();

				var mqttFactory = new MqttFactory();
				MqttClient = mqttFactory.CreateMqttClient();
				MqttClient.ApplicationMessageReceivedAsync += async e =>
				{
					try
					{
						var message = e.ApplicationMessage;
						if (message.Topic == "event")
						{
							var payload = e.ApplicationMessage.PayloadSegment;

							var request = JsonSerializer.Deserialize<DetectionDTO>(payload);
							if (request == null || request.Image == null)
							{
								return;
							}
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

							var modelPath = string.Empty;
							string[] s = null;
							switch (request.Model)
							{
								case "coco":
									modelPath = $"{ROOT}/models/coco.onnx";
									break;
								case "fire":
									modelPath = $"{ROOT}/models/fire.onnx";
									s = new string[] { "fire", "smoke" };
									break;
							}
							using var model = YoloV8Predictor.Create(modelPath, labels: s, useCuda: true);

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
							if (!predictions.Any())
							{
								await Task.CompletedTask;
							}

							var boundingBoxes = new List<BoundingBoxDTO>();
							foreach (var prediction in predictions)
							{
								var originalImageHeight = input.Height;
								var originalImageWidth = input.Width;
								var x = (int)Math.Max(prediction.Rectangle.X, 0);
								var y = (int)Math.Max(prediction.Rectangle.Y, 0);
								var width = (int)Math.Min(originalImageWidth - x, prediction.Rectangle.Width);
								var height = (int)Math.Min(originalImageHeight - y, prediction.Rectangle.Height);
								var text = $"{prediction.Label.Name}: {prediction.Score}";
								var size = TextMeasurer.Measure(text, new TextOptions(FONT));
								input.Mutate(d => d.Draw(Pens.Solid(Color.Yellow, 2), new Rectangle(x, y, width, height)));
								input.Mutate(d => d.DrawText(new TextOptions(FONT) { Origin = new Point(x, (int)(y - size.Height - 1)) }, text, Color.Yellow));
								var boundingBox = new BoundingBoxDTO
								{
									X = x,
									Y = y,
									Width = width,
									Height = height,
									Label = prediction.Label.Name,
									Confidence = prediction.Score
								};
								boundingBoxes.Add(boundingBox);
							}

							var eventImagePath = Path.Combine(ROOT, "images", $"{Guid.NewGuid()}.jpeg");
							input.Save(eventImagePath);
							var eventDTO = new EventDTO
							{
								Date = request.Date,
								UserId = request.UserId,
								CameraId = request.CameraId,
								Path = eventImagePath
							};

							var createdEventDTO = await _eventRepositroy.Create(eventDTO);
							var createdEvent = JsonSerializer.Serialize<CreatedEventDTO>(new CreatedEventDTO
							{
								Id = createdEventDTO.Id,
								CameraId = createdEventDTO.CameraId
							});
							using (var mqttClient = mqttFactory.CreateMqttClient())
							{
								var mqttClientOptions = new MqttClientOptionsBuilder()
									.WithTcpServer("ictrobot.hknu.ac.kr", 8085)
									.Build();
								await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
								var applicationMessage = new MqttApplicationMessageBuilder()
									.WithTopic("event/create")
									.WithPayload(createdEvent)
									.Build();
								await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
								await mqttClient.DisconnectAsync();
							}

							foreach (var boundingBox in boundingBoxes)
							{
								boundingBox.EventId = createdEventDTO.Id;
							}
							var count = await _boundingBoxRepository.Create(boundingBoxes);
							if (count <= 0)
							{
								throw new Exception($"BoundingBox를 저장하는데 실패했습니다.");
							}

							if (File.Exists(inputImagePath))
							{
								File.Delete(inputImagePath);
							}

						}
						else if (message.Topic == "event/video/create")
						{
							var payload = e.ApplicationMessage.PayloadSegment;

							var request = JsonSerializer.Deserialize<CreateEventVideoDTO>(payload);
							if (request == null)
							{
								return;
							}
							var eventDTOs = (await _eventRepositroy.GetAll(request.EventIds)).ToList();
							if (!eventDTOs.Any())
							{
								return;
							}

							var imagePaths = new List<string>();
							foreach (var eventDTO in eventDTOs)
							{
								if (!string.IsNullOrEmpty(eventDTO.Path))
								{
									imagePaths.Add(eventDTO.Path);
								}
							}

							if (imagePaths.Any())
							{
								var identifier = Guid.NewGuid().ToString();
								for (int i = 0; i < imagePaths.Count; i++)
								{
									var destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{identifier}_{i + 1}.jpeg");
									File.Copy(imagePaths[i], destinationPath);
									//if (File.Exists(imagePaths[i]))
									//{
									//	File.Delete(imagePaths[i]);
									//}
								}

								var videoPath = $"{Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos", $"{Guid.NewGuid()}")}.mp4";
								var args = $"-framerate 1 -i {Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", identifier)}_%d.jpeg -c:v libx264 -r 30 -pix_fmt yuv420p {videoPath}";
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

								var processOutput = string.Empty;
								while ((processOutput = ffMpeg.StandardError.ReadLine()) != null)
								{
									Console.WriteLine(processOutput);
								}

								var createdVideoDTO = await _eventVideoRepository.Create(new EventVideoDTO
								{
									Path = videoPath
								});

								foreach (var eventDTO in eventDTOs)
								{
									eventDTO.EventVideoId = createdVideoDTO.Id;
									await _eventRepositroy.Update(eventDTO);
								}

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

				while (!stoppingToken.IsCancellationRequested)
				{
					await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
				}
			}
		}
	}
}
