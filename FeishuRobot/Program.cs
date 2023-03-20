using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeishuRobot
{
    class Program
    {
        static void Main(string[] args)
        {
            Robot robot = new Robot();
            robot.Run();
            Console.ReadKey();
        }
    }
}
