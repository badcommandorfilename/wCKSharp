using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wCKSharp
{
    public class wCKMaster
    {
        SerialPort _port;

        public wCKMaster(SerialPort port)
        {
            _port = port;
        }

        private static byte checksum(params byte[] b)
        {
            return (byte)(b.Aggregate((i, a) => (byte)(i ^ a)) & 0x7F);
        }

        Task<byte[]> sendCmd(byte[] cmd, int n = 0)
        {
            var result = new TaskCompletionSource<byte[]>();
            lock (_port)
            {
                var response = new byte[n];
                SerialDataReceivedEventHandler cb = new SerialDataReceivedEventHandler((o, e) => { });
                cb = new SerialDataReceivedEventHandler((o, e) =>
                {
                    _port.DataReceived -= cb;
                    _port.Read(response, 0, n);
                    result.TrySetResult(response);
                });

                _port.DataReceived += cb;

                _port.Write(cmd, 0, cmd.Length);
            }

            return result.Task;
        }

        public async Task<byte[]> move(int id, int target, int torque = 4)
        {
            byte b1 = 0xFF;
            byte b2 = (byte)((id & 31) | (torque << 5));
            byte b3 = (byte)target;
            byte chk = checksum(b2, b3);

            var cmd = new byte[] { b1, b2, b3, chk };

            return await sendCmd(cmd, 2);
        }


        public async Task<byte[]> moveFine(int id, int target, int torque = 4)
        {
            byte b1 = 0xFF;
            byte b2 = (byte)(7 << 5);
            byte b3 = 200;
            byte b4 = (byte)(id & 31);
            byte b5 = (byte)(torque & 255);
            byte b6 = (byte)((target >> 5) & (0x07)); //High 3 
            byte b7 = (byte)((target << 1) & (0xF7)); //Low 7
            byte chk = checksum(b2, b3, b4, b5, b6, b7);
            var cmd = new byte[] { b1, b2, b3, b4, b5, b6, b7, chk };
            Console.WriteLine("Target {0}[{1},{2}]", target, b6, b7);

            return await sendCmd(cmd, 2);
        }

        public async Task<int> moveTo(int id, int target, int torque = 4)
        {
            while(true)
            {
                Console.WriteLine("Target {0}", target);
                var response = await move(id, target, torque);
                int position = response[1];
                Console.WriteLine("Position {0}[{1},{2}]", position, response[0], response[1]);
                if (position == target)
                {
                    return position;
                }
            }
        }

        public async Task<int> moveToFine(int id, int target, int torque = 0, int tol = 10)
        {
            while (true)
            {
                Console.WriteLine("Target {0}", target);
                var response = await moveFine(id, target, torque);
                var h = ((response[0] & 0x07) << 7);
                var l = (response[1] >> 1);
                var position = h + l;
                Console.WriteLine("Position {0}[{1},{2}]", position, h, l);
                if (Math.Abs(position - target) < tol)
                {
                    return position;
                }
            }
        }
    }
}
