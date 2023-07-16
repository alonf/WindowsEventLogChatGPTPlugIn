using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics.Eventing.Reader;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using YamlDotNet.Serialization;
using System.Xml;
using Newtonsoft.Json;

var argsWithUrls = new[] { "--urls", "http://localhost:5000" }.Concat(args).ToArray();

var builder = WebApplication.CreateBuilder(argsWithUrls);
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy =>
    policy.AllowAnyOrigin() // Allow requests from any origin
        .AllowAnyMethod() // Allow all HTTP methods
        .AllowAnyHeader()); // Allow all headers

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Add mappings for .yaml
        [".yaml"] = "application/x-yaml",
        [".yml"] = "application/x-yaml",
        [".json"] = "application/json",
        [".png"] = "application/png"
    })
});

app.UseSwagger();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();




app.MapGet("/events", (string logName, string query) =>
    {
        try
        {
            //validate log name
            switch (logName)
            {
                case "Application":
                case "Security":
                case "Setup":
                case "System":
                case "ForwardedEvents":
                    break;
                default:
                    return Results.BadRequest("Invalid log name");
            }

            // Create an EventLogQuery instance.
            var eventsQuery = new EventLogQuery("Application", PathType.LogName, query);

            // Create an EventLogReader instance.
            var logReader = new EventLogReader(eventsQuery);

            // Read the events in the log.
            var events = new List<EventEntry>();

            int nEvents = 0;
            while (logReader.ReadEvent() is { } record)
            {
                events.Add(new EventEntry {Id = record.Id, Json =ConvertXmlToJson(record.ToXml())});
                if (++nEvents >= 5)
                {
                    break;
                }
            }

            // Return the events.
            return Results.Ok(events);
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as appropriate for your application.
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }).WithName("GetEvents")
    .WithDisplayName("Get Events")
    .WithDescription("Retrieve log events from the Windows event log.")
    .WithTags("Windows Event Log", "Events", "Log")
    .WithOpenApi(operation =>
    {
        operation.Parameters.Clear();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "logName",
            In = ParameterLocation.Query,
            Required = true,
            Description = "One of the following log names: Application, Security, Setup, System, ForwardedEvents",
            Schema = new OpenApiSchema
            {
                Type = "string",
            }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "query",
            In = ParameterLocation.Query,
            Required = true,
            Description = "valid XPath query for the Windows event log",
            Schema = new OpenApiSchema { Type = "string" }
        });
        return operation;
    }).Produces<List<EventEntry>>();

app.MapGet("/swagger/v1/swagger.yaml", (ISwaggerProvider swaggerProvider) =>
{
    var swagger = swaggerProvider.GetSwagger("v1");

    var serializer = new SerializerBuilder().Build();
    var yaml = serializer.Serialize(swagger);

    return Task.FromResult(Results.Content(yaml, "application/yaml"));
}).ExcludeFromDescription(); 

app.Run();

string ConvertXmlToJson(string xml)
{
    XmlDocument doc = new XmlDocument();
    doc.LoadXml(xml);
    string json = JsonConvert.SerializeXmlNode(doc);
    return json;
}
public record EventEntry
{
    public int Id { get; set; }
    public string Json { get; init; } = "";
}