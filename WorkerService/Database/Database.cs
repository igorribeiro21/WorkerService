using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using WebsiteStatus;
using Microsoft.Extensions.Logging;

namespace WorkerService
{
    public class Database
    {
        private readonly ILogger<Worker> _logger;

        public Database(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public DataTable ExecuteConsult()
        {
            DataTable dt = new DataTable();
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();            

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
    }
}
