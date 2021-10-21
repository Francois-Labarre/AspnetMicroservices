using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Discount.Grpc.Extensions
{
    public static class HostExtensions 
    {
        public static IHost MigrateDatabase<TContext>(this IHost host, int? retry = 0)
        {
            int retryForAvailibility = retry.Value;
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<TContext>>();

                try
                {
                    logger.LogInformation("Migrating postgresql database");

                    using var connection = new NpgsqlConnection(
                        configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
                    connection.Open();

                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                    command.CommandText = "Drop table if exists Coupon";
                    command.ExecuteNonQuery();

                    command.CommandText = @"create table Coupon(Id serial primary key, ProductName varchar(24), Description text, Amount int)";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into Coupon (ProductName, Description, Amount) values ('IPhone X', 'IPhone Discount', 150);";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into Coupon (ProductName, Description, Amount) values ('Samsumg 10', 'Samsung Discount', 100);";
                    command.ExecuteNonQuery();

                    logger.LogInformation("Migrated postgresql database.");
                }
                catch(NpgsqlException ex)
                {
                    logger.LogInformation(ex, "An error occured while migrating the postgresql database");

                    if(retryForAvailibility < 50)
                    {
                        retryForAvailibility++;
                        System.Threading.Thread.Sleep(2000);
                        MigrateDatabase<TContext>(host, retryForAvailibility);
                    }
                }
            }
            return host;
        }
    }

}
