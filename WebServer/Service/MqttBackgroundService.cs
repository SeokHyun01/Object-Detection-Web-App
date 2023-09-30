using MQTTnet.Client;
using MQTTnet;
using System.Text.Json;
using Yolov8Net;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using Models;
using Business.Repository.IRepository;

namespace WebServer.Service
{
	public class MqttBackgroundService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private IEventRepository? _eventRepositroy = null;
		private IBoundingBoxRepository? _boundingBoxRepository = null;
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

				var mqttFactory = new MqttFactory();
				MqttClient = mqttFactory.CreateMqttClient();
				MqttClient.ApplicationMessageReceivedAsync += async e =>
				{
					Task.Run(async () =>
					{
						var message = e.ApplicationMessage;
						if (message.Topic == "redetection")
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
								inputImagePath = Path.Combine(ROOT, "redetections", $"{request.Date}_{request.UserId}_{request.CameraId}.jpeg");
								using (var fileStream = new FileStream(inputImagePath, FileMode.Create))
								{
									await stream.CopyToAsync(fileStream);
								}
							}

                            using var model = YoloV8Predictor.Create($"{ROOT}/models/yolov8l.onnx");
                            // using var model = YoloV8Predictor.Create($"{ROOT}/models/yolov8l.onnx", useCuda: true);
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

							var eventImagePath = Path.Combine(ROOT, "events", $"{request.Date}_{request.UserId}_{request.CameraId}.jpeg");
							input.Save(eventImagePath);
							var eventDTO = new EventDTO
							{
								Date = request.Date,
								UserId = request.UserId,
								CameraId = request.CameraId,
								Path = eventImagePath
							};
							var createdEventDTO = await _eventRepositroy.Create(eventDTO);

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
					});
				};

				var mqttClientOptions = new MqttClientOptionsBuilder()
					.WithTcpServer("ictrobot.hknu.ac.kr", 8085)
					.Build();
				await MqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

				var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
					.WithTopicFilter(f => { f.WithTopic("redetection"); })
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
