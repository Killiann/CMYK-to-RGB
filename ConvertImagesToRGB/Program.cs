using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using ImageMagick;

namespace ConvertImagesToRGB              
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                bool? integratedSecurity = null;
                bool hasConnected = false;
                string dataSource, userId, password, initialCatalog;
                //table namew
                //column name

                while (!hasConnected)
                {
                    Console.Clear();
                    dataSource = "";
                    userId = "";
                    password = "";
                    initialCatalog = "";                    
                    integratedSecurity = null;
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                    //read from user
                    Console.WriteLine("Connection String Details: ");
                    Console.WriteLine("==========================");
                    while (integratedSecurity == null)
                    {
                        Console.WriteLine("Integrated Security? (Y/N)");
                        var x = Console.ReadLine();
                        if (x.ToLower() == "y") integratedSecurity = true;
                        else if (x.ToLower() == "n") integratedSecurity = false;
                        else Console.WriteLine("Invalid input please enter Y for yes and N for no.");
                    }
                    Console.Write("Data Source     = ");
                    dataSource = Console.ReadLine();
                    if ((bool)!integratedSecurity)
                    {
                        Console.Write("User ID         = ");
                        userId = Console.ReadLine();
                        Console.Write("Password        = ");
                        password = Console.ReadLine();
                    }
                    Console.Write("Initial Catalog = ");
                    initialCatalog = Console.ReadLine();

                    //setup variables
                    builder.DataSource = dataSource;
                    builder.InitialCatalog = initialCatalog;
                    if ((bool)integratedSecurity)
                        builder.IntegratedSecurity = true;
                    else
                    {
                        builder.UserID = userId;
                        builder.Password = password;
                    }
                    Console.Write("Connecting to SQL server ... ");

                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        try
                        {
                            connection.Open();
                            hasConnected = true;
                            Console.WriteLine("done.");

                            List<Byte[]> images = new List<byte[]>();
                            List<int> clientIds = new List<int>();
                            string table = "";
                            string byte_col = "";
                            string id_col = "";
                            bool foundTable = false;

                            while (!foundTable)
                            {
                                Console.Write("Table name      = ");
                                table = Console.ReadLine();                                
                                Console.Write("Photo col name  = ");
                                byte_col = Console.ReadLine();
                                Console.Write("ID column name  = ");
                                id_col = Console.ReadLine();

                                //pull client data
                                String SQL = String.Format("SELECT [{2}], [{3}] FROM [{0}].[dbo].[{1}]", initialCatalog, table, byte_col, id_col);                              
                                Console.Write("Fetching client data ... ");
                                try
                                {
                                    using (SqlCommand command = new SqlCommand(SQL, connection))
                                    {

                                        using (SqlDataReader reader = command.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                if (reader.GetValue(0) != DBNull.Value)
                                                {
                                                    byte[] buffer = null;
                                                    buffer = (byte[])reader[0];
                                                    images.Add(buffer);
                                                    clientIds.Add(Convert.ToInt32(reader[1]));
                                                }
                                            }
                                            foundTable = true;
                                            Console.WriteLine("done.");
                                            Console.WriteLine("Client Count: " + images.Count());
                                            Console.WriteLine("=====================================");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Command could not be executed try again.");
                                }
                            }

                            //convert images
                            if (images.Count > 0)
                            {
                                for (int i = 0; i < images.Count; i++)
                                {
                                    byte[] byteArray = images[i];
                                    if (byteArray.Length > 0)
                                    {
                                        using (MagickImage image = new MagickImage(byteArray))
                                        {
                                            if (image.ColorSpace.ToString() == "CMYK")
                                            {
                                                Console.WriteLine("CMYK Image for client: ID: " + clientIds[i]);
                                                MagickImageInfo info = new MagickImageInfo(byteArray);

                                                //download original image(CMYK)
                                                Bitmap bmp = image.ToBitmap();
                                                string fileName = "C:\\Users\\k.comerford\\Desktop\\SavedImages\\original_" + clientIds[i];
                                                if (info.Format == MagickFormat.Jpeg)
                                                {
                                                    fileName += ".jpeg";
                                                    bmp.Save(fileName, ImageFormat.Jpeg);
                                                }
                                                else if (info.Format == MagickFormat.Png)
                                                {
                                                    fileName += ".png";
                                                    bmp.Save(fileName, ImageFormat.Png);
                                                }

                                                //strip color profiles + convert to RGB
                                                image.Strip();
                                                image.AddProfile(ColorProfile.USWebCoatedSWOP);
                                                image.AddProfile(ColorProfile.SRGB);
                                                byteArray = image.ToByteArray();

                                                //add update command to query
                                                Console.Write("Updating record ... ");
                                                SqlCommand command = new SqlCommand(String.Format("UPDATE [{0}].[dbo].[{1}] SET [{2}] = 0x{0} where [{3}] = {4};", initialCatalog, table, byte_col, id_col, BitConverter.ToString(byteArray).Replace("-", "").ToLower(), clientIds[i]), connection);
                                                Console.Write(command.ExecuteNonQuery().ToString() + " rows affected. ... ");
                                                Console.WriteLine("done.");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("No image for ID: " + clientIds[i]);
                                    }
                                }
                                Console.WriteLine("Checked all images.");
                                connection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Could not connect to the server.");
                            Console.WriteLine("Press Enter to try again.");
                            Console.WriteLine("Press Any other key to exit.");
                            ConsoleKeyInfo input;
                            input = Console.ReadKey(true);
                            if (input.Key != ConsoleKey.Enter) Environment.Exit(0);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
        }
    }
}
