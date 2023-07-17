using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics.Eventing.Reader;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using YamlDotNet.Serialization;
using System.Xml;
using Microsoft.OpenApi.Any;
using Newtonsoft.Json;

const int maxGptTokens = 4096;
char[] delimiters = { ' ', '.', ',', ';', '!', '?', '<', '>', ':', '\'', '\"', '\n', '\t' }; // Add more delimiters as needed


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

app.UseStaticFiles();
app.UseSwagger();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();


app.MapGet("/events", (string logName, string query, int? pageSize, int? pageNumber) =>
    {
        if (pageSize is null or < 1)
            pageSize = 5;
        
        if (pageNumber is null or < 1)
            pageNumber = 1;

        int actualPageSize = pageSize.Value;
        int actualPageNumber = pageNumber.Value;


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

            long offset = actualPageSize * (actualPageNumber - 1);

            // Create an EventLogQuery instance.
            var eventsQuery = new EventLogQuery(logName, PathType.LogName, query);

            // Create an EventLogReader instance.
            var logReader = new EventLogReader(eventsQuery);

            // Seek to the offset
            logReader.Seek(SeekOrigin.Begin, offset);

            // Read the events
            var queryResult = new EventQueryResult()
            {
                PageNumber = actualPageNumber,
                PageSize = actualPageSize,
                HasMore = true,
                HasPageSizeTruncated = false
            };

            for (int i = 0; i < pageSize; i++)
            {
                EventRecord record = logReader.ReadEvent();
                if (record == null)
                {
                    queryResult.HasMore = false;
                    break;
                }
                queryResult.Events.Add(new EventEntry { Id = record.Id, Json = ConvertXmlToJson(record.ToXml()) });

                if (GetResultEstimationTokenCount(queryResult.Events) > maxGptTokens)
                {
                    queryResult.Events.RemoveAt(queryResult.Events.Count - 1);
                    queryResult.PageSize = i;
                    queryResult.HasPageSizeTruncated = true;
                    break;
                }
            }

            // Return the events.
            return Results.Ok(queryResult);
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
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "pageSize",
            In = ParameterLocation.Query,
            Required = false,
            Description = "Number of events to return",
            Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(5) }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "pageNumber",
            In = ParameterLocation.Query,
            Required = false,
            Description = "Page number of events to return",
            Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(1) }
        });

        return operation;
    }).Produces<EventQueryResult>();

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

int GetResultEstimationTokenCount(IList<EventEntry> events)
{
    var count = events.Sum(e=>e.Json.Count(c => delimiters.Contains(c)) + 20); //20 for additional information tokens for each entry
    return (int)(count * 1.4); //factor the fact that some tokens are longer than 1 character
}

public record EventEntry
{
    public int Id { get; set; }
    public string Json { get; init; } = "";
}

public record EventQueryResult
{
    public List<EventEntry> Events { get; init; } = new();
    public bool HasMore { get; set; }
    public int PageNumber { get; init; }
    public bool HasPageSizeTruncated { get; set; }
    public int PageSize { get; set; }
}