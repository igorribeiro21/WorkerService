using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;
using WorkerService;
using WorkerService.Convert;

namespace WebsiteStatus
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private HttpClient client;

        public Worker(ILogger<Worker> logger,IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            client = new HttpClient();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            client.Dispose();
            return base.StopAsync(cancellationToken);
        }

       

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Database db = new Database(_logger, Configuration);
            Conversions conversions = new Conversions();

            while (!stoppingToken.IsCancellationRequested)
            {                
                DataTable consult = db.ExecuteConsult();

                string json = conversions.DataTable_JSON_StringBuilder(consult);                

                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                
                var urlApi = "http://localhost:3333/post";
                
                var result = await client.PostAsync(urlApi, stringContent);

                if (result.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Dados enviados com sucesso");
                }
                else
                {
                    _logger.LogInformation("Ocorreu um erro ao enviar os dados. Status Code {StatusCode}", result.StatusCode);
                }

                await Task.Delay(60000 * 5, stoppingToken);
            }
        }       

        
    }
}
