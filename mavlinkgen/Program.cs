/*
The MIT License (MIT)

Copyright (c) 2013, David Suarez

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

/*
 * E:\Users\RobThePyro\Docs\GitHub\mavlink.net\mavlinkgen\bin\Debug>mavlinkgen.exe
-output="E:\Users\RobThePyro\Docs\GitHub\mavlink.net\mavlink.net\GeneratedMessag
es.cs" "E:\Users\RobThePyro\Docs\GitHub\SolarForce1\CAN Embedded\MAVLink\mavlink
_generator\message_definitions\v1.0\solarCar.xml"
 */

using System;
using System.IO;
using System.Xml;
using MavLinkObjectGenerator;

namespace MavLinkGen
{
    class MainClass
    {
        public static int Main(string[] args)
        {
            try
            {
                Configuration c = new Configuration(args);
                c.CheckValid();

                foreach (string file in c.Files)
                {
                    try
                    {
                        new XmlParser(file).Generate(c.OutputFile, c.GetGenerator());
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error in [{0}]: {1} ", file, ex.Message);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }
    }
}
