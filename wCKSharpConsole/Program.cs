using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wCKSharp;

namespace wCKSharpConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var port = new SerialPort("COM2", 115200))
            {
                port.Open();
                var g = new wCKMaster(port);

                Pose(g, new Random().Next(100));
                Task.WaitAll(Task.Delay(100));
                for (int t = 0; t < 5; t++)
                {
                    for (int i = 0; i < 1000; i += 10)
                    {
                        Pose(g,i);
                        Task.WaitAll(Task.Delay(1000));
                    }
                }
            }
        }

        private static async void Pose(wCKMaster g, int i)
        {
            var r1 = g.moveFine(00, i);

            Task.WaitAll(Task.Delay(100), r1);
            Console.WriteLine(String.Format("{0}", r1.Result));
        }
    }
}
