using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace WebsiteStatus
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private HttpClient client;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
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

        public DataTable ExecuteConsult()
        {
            DataTable dt = new DataTable();
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                //builder.DataSource = "localhost\\SQLEXPRESS";
                //builder.UserID = "sa";
                //builder.Password = "Saobento21";
                //builder.InitialCatalog = "Teste";

                builder.ConnectionString = Configuration.GetConnectionString("TesteDB");

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    String sql = "exec buscaDados";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {

                            dt.Load(reader);
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                var id = dt.Rows[i]["id"].ToString();
                                var nome = dt.Rows[i]["nome"].ToString();
                                var email = dt.Rows[i]["email"].ToString();
                                string frase = "o usuário de id: " + id + " e de nome: " + nome + " e email: " + email;

                                _logger.LogInformation(frase);


                                //sql = "exec pr_inserirDados @id= " + Convert.ToInt32(id) + " , @nome= '" + nome + "' , @email= '" + email + "'";
                                //SqlCommand commandInsert = new SqlCommand(sql, connection);
                                //commandInsert.ExecuteNonQuery();


                            }
                            //_logger.LogInformation("{0}", reader.GetString(0));

                            connection.Close();

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.ToString());
            }

            return dt;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DataTable consult = ExecuteConsult();

                string json = DataTable_JSON_StringBuilder(consult);

                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                var url = "https://www.iamtimcorey.com";
                var urlApi = "http://localhost:3333/post";


                var result = await client.GetAsync(url);
                var postResult = await client.PostAsync(urlApi, stringContent);

                if (postResult.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Dados enviados com sucesso");
                }
                else
                {
                    _logger.LogInformation("Ocorreu um erro ao enviar os dados. Status Code {StatusCode}", postResult.StatusCode);
                }


                if (result.IsSuccessStatusCode)
                {
                    _logger.LogInformation("O website está no ar. Status Code {StatusCode}", result.StatusCode);
                }
                else
                {
                    _logger.LogInformation("O website está fora do ar. Status Code {StatusCode}", result.StatusCode);
                }
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(60000 * 5, stoppingToken);
            }
        }

        public static string DataTable_JSON_StringBuilder(DataTable tabela)
        {
            var JSONString = new StringBuilder();
            if (tabela.Rows.Count > 0)
            {
                JSONString.Append("[");
                for (int i = 0; i < tabela.Rows.Count; i++)
                {
                    JSONString.Append("{");
                    for (int j = 0; j < tabela.Columns.Count; j++)
                    {
                        if (j < tabela.Columns.Count - 1)
                        {
                            JSONString.Append("\"" + tabela.Columns[j].ColumnName.ToString() + "\":" + "\"" + tabela.Rows[i][j].ToString() + "\",");
                        }
                        else if (j == tabela.Columns.Count - 1)
                        {
                            JSONString.Append("\"" + tabela.Columns[j].ColumnName.ToString() + "\":" + "\"" + tabela.Rows[i][j].ToString() + "\"");
                        }
                    }
                    if (i == tabela.Rows.Count - 1)
                    {
                        JSONString.Append("}");
                    }
                    else
                    {
                        JSONString.Append("},");
                    }
                }
                JSONString.Append("]");
            }
            return JSONString.ToString();

        }
    }
}
