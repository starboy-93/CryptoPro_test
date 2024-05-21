using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoPro_test
{
    class Program
    {
        static string connectionString = @"Data Source=GEORGE-PC\SQLEXPRESS;Encrypt=no;User ID=;Password=;Database=CryptoPro_test;Trusted_Connection=True;";

        static void Main()
        {
            CreateTableIfNotExists();

            if (CheckConnection(connectionString))
            {
                Console.WriteLine("Подключение к БД установлено!");
            }
            else
            {
                Console.WriteLine("Ошибка подключения к БД!");
            }

            Console.WriteLine("Сколько будем запускать экземпляров? Введите число от 2 до 20:");

            int N = GetIntFromUser(); //количество одновременно запущенных экземпляров
            int totalRecords = 1000; //общее количество записей

            for (int i = 0; i < N; i++)
            {
                int processId = i + 1;
                Thread thread = new Thread(() => CreateRecords(processId, totalRecords, N));
                thread.Start();
            }
            Console.WriteLine($"В БД добавлено {totalRecords} записей в {N} процессе/процессах.");
        }



        static void CreateTableIfNotExists()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();

                //проверяем существование таблицы
                command.CommandText = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Records' AND xtype='U') " +
                                      "CREATE TABLE Records (Number INT PRIMARY KEY, CreationDateTime DATETIME, ProcessId INT)";

                command.ExecuteNonQuery();
            }
        }


        static void CreateRecords(int processId, int totalRecords, int N)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction = connection.BeginTransaction();
                command.Transaction = transaction;

                try
                {
                    int recordsPerProcess = totalRecords / N + (processId <= totalRecords % N ? 1 : 0);

                    for (int i = 0; i < recordsPerProcess; i++)
                    {
                        command.CommandText = "INSERT INTO Records (Number, CreationDateTime, ProcessId) VALUES (@Number, @CreationDateTime, @ProcessId)";
                        command.Parameters.AddWithValue("@Number", GetUniqueNumber());
                        command.Parameters.AddWithValue("@CreationDateTime", DateTime.Now);
                        command.Parameters.AddWithValue("@ProcessId", processId);
                        command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    transaction.Rollback();
                }
            }
        }

        static int GetUniqueNumber()
        {
            return Guid.NewGuid().GetHashCode();
        }
        static bool CheckConnection(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка: " + ex.Message);
                    return false;
                }
            }
        }

        static int GetIntFromUser()
        {
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out int num) && (num >= 2 && num <= 20))
                {
                    return num;
                }
                else
                {
                    Console.Write("Ошибка ввода. Пожалуйста, введите корректное число от 2 до 20:\n");
                }
            }
        }
    }
}
