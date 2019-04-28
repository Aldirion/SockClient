using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;


namespace SockClient
{

    class TCPClient
    {
        //задание основных переменных
        static string log, pass;    //логин, пароль
        static int port = 8005;     //порт подключения
        static int localport = 7000;//локальный порт для получения ответов от серверов
        static string address = ""; //ip-адрес
        //функция для ручного ввода ip-адреса
        static void Main_interface(out int port, out string address)
        {
            Console.Clear();
            Console.WriteLine("Введите адрес хоста: \n");
            address = Console.ReadLine();
            Console.WriteLine("Введите номер порта: \n");
            port = Convert.ToInt16(Console.ReadLine());
        }

        static void Main(string[] args)
        {
            bool continueFlag = true;
            do
            {
                Console.Clear();
                Console.WriteLine("Введите логин:");
                log = Console.ReadLine();
                //идентификация
                if (log.Equals("admin"))
                {
                    Console.WriteLine("Введите пароль для входа в приложение:");
                    ConsoleKeyInfo key;
                    do
                    {
                        key = Console.ReadKey(true);
                        if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                        {
                            pass += key.KeyChar;
                            Console.Write("*");
                        }
                        else
                        {
                            //"*" вместо символов пароля
                            if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                            {
                                pass = pass.Substring(0, (pass.Length - 1));
                                Console.Write("\b \b");
                            }
                            else if (key.Key == ConsoleKey.Enter)
                            {
                                Console.WriteLine();
                                break;
                            }
                        }
                    } while (true);
                    //аутентификация
                    if (!pass.Equals("admin"))
                    {
                        Console.WriteLine("Пароль введен неверно.");
                        return;
                    }
                }
                else return;
                //Main_interface(out port, out address);
                try
                {
                    Console.Write("Введите путь к файлу проекта: ");
                    string prjpath = Console.ReadLine();    //путь к файлу проекта поиска и подсчета КС
                    string path = "";                       //общая переменная пути
                    Console.Write("Введите путь к файлу с адресами");
                    string addrpath = Console.ReadLine();   //путь к файлу с адресами серверов для опроса
                    using (StreamReader sr = new StreamReader(addrpath, System.Text.Encoding.Default))      //читаем поток байтов (файл)
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)  //читаем файл построчно
                        {
                            address = "";
                            port = 0;
                            int it = 0;
                            //записываем первую лексему в переменную адрес
                            while (true)
                            {
                                if (line[it] != ' ')
                                {
                                    address += line[it];
                                    it++;
                                }
                                else break;
                            }
                            //записываем вторую лексему в переменную порт
                            port = Convert.ToInt32(line.Substring((it + 1)));

                            Console.WriteLine("Отправляем файл...");
                            TcpClient eclient = new TcpClient(address, port);
                            NetworkStream writerStream = eclient.GetStream();
                            BinaryFormatter format = new BinaryFormatter();
                            byte[] buf = new byte[1024];
                            int count;
                            FileStream fs = new FileStream(prjpath, FileMode.Open);
                            BinaryReader br = new BinaryReader(fs);
                            long k = fs.Length;//Размер файла.
                            format.Serialize(writerStream, k.ToString());//Вначале передаём размер
                            while ((count = br.Read(buf, 0, 1024)) > 0)
                            {
                                format.Serialize(writerStream, buf);//А теперь в цикле по 1024 байта передаём файл
                            }
                            eclient.Close();
                            Console.WriteLine("Файл успешно отправлен. Ждем ответа от сервера {0}:{1}", address, port);
                            fs.Close();

                            //Слушаем соединения на localport
                            TcpListener clientListener = new TcpListener(localport);
                            clientListener.Start();
                            //Задаем путь для сохранения файла КС в формате *address_port.md5s* пример: 127_0_0_1_8006.md5s
                            path = "C:\\Users\\sokol\\source\\CourseProject\\DATA\\" + address.Replace(".", "_") + "_" + Convert.ToString(port) + ".md5s";
                            TcpClient client = clientListener.AcceptTcpClient();
                            NetworkStream readerStream = client.GetStream();
                            BinaryFormatter outformat = new BinaryFormatter();
                            fs = new FileStream(path, FileMode.OpenOrCreate);
                            BinaryWriter bw = new BinaryWriter(fs);
                            count = int.Parse(outformat.Deserialize(readerStream).ToString());//Получаем размер файла
                            int i = 0;
                            for (; i < count; i += 1024)//Цикл пока не дойдём до конца файла
                            {

                                buf = (byte[])(outformat.Deserialize(readerStream));//Собственно читаем из потока и записываем в файл
                                bw.Write(buf);
                            }
                            //высвобождение памяти
                            buf = null;
                            GC.Collect();
                            bw.Close();
                            fs.Close();
                            clientListener.Stop();
                            client.Close();
                            //Console.ReadKey();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }


                if (Char.ToUpper(GetKeyPress("Продолжить работу программы? (Y/N): ", new Char[] { 'Y', 'N' })) == 'N')
                    continueFlag = false;
            } while (continueFlag == true);
        }


        private static Char GetKeyPress(String msg, Char[] validChars)
        {
            ConsoleKeyInfo keyPressed;
            bool valid = false;

            Console.WriteLine();
            do
            {
                Console.Write(msg);
                keyPressed = Console.ReadKey();
                Console.WriteLine();
                if (Array.Exists(validChars, ch => ch.Equals(Char.ToUpper(keyPressed.KeyChar))))
                    valid = true;

            } while (!valid);
            return keyPressed.KeyChar;
        }
    }
}
