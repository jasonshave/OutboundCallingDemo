using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using OutboundCalling.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(new CallAutomationClient(builder.Configuration["ACS:ConnectionString"]));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/calls", async (CallRequest request, CallAutomationClient client) =>
{
    var applicationId = new CommunicationUserIdentifier("<insert_your_id>");
    var callSource = new CallSource(applicationId)
    {
        CallerId = new PhoneNumberIdentifier(request.Source),
        DisplayName = request.DisplayName
    };

    var targets = new List<CommunicationIdentifier>()
    {
        new PhoneNumberIdentifier(request.Destination)
    };

    var createCallOptions = new CreateCallOptions(callSource, targets,
        new Uri(builder.Configuration["VS_TUNNEL_URL"] + "api/callbacks"));

    await client.CreateCallAsync(createCallOptions);
});

app.MapPost("/api/callbacks", async (CloudEvent[] events, CallAutomationClient client, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in events)
    {
        var @event = CallAutomationEventParser.Parse(cloudEvent);
        if (@event is CallConnected callConnected)
        {
            logger.LogInformation($"The call is connected! CallConnectionID: {callConnected.CallConnectionId}");
        }

        if (@event is CallDisconnected callDisconnected)
        {
            logger.LogInformation($"The call is disconnected! CallConnectionID: {callDisconnected.CallConnectionId}");
        }
    }
});

app.Run();
