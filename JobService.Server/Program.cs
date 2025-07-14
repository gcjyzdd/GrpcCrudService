using JobService.Server;

var app = ApplicationFactory.CreateApplication(args);
await app.RunAsync();
