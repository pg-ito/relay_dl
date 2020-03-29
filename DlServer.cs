using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace relay_dl
{
    class DlServer
    {
        TcpListener tcp;
        private const string eol = "\r\n";
        private const string indexFile = "/index.html";
        Int32 bufferSize = 4096;
        string docroot = "docroot";
        public DlServer() {
            IPAddress ip = IPAddress.Any;
            tcp = new TcpListener(ip , 9821);
            tcp.Start();
            Console.WriteLine($"Listening {ip} ...");


            while (true)
            {
                TcpClient client = tcp.AcceptTcpClient();

                NetworkStream ns = client.GetStream();

                while (!ns.DataAvailable) { };

                Byte[] bytes = new Byte[client.Available];

                ns.Read(bytes, 0, bytes.Length);
                String data = Encoding.UTF8.GetString(bytes);

                if (!Regex.IsMatch(data, "^GET"))
                {
                    continue;
                }
                Console.WriteLine($"readData {data}");
                string path = GetPath(data);
                short status = 200;



                if (!File.Exists(path))
                {
                    status = 404;
                    WriteHeader(status, ns);
                    byte[] body = Encoding.UTF8.GetBytes("FileNotFound" + eol + eol);
                    ns.Write(body, 0, body.Length);
                    Disconnect(ns);
                    continue;
                }

                byte[] header = Encoding.UTF8.GetBytes($"HTTP/1.1 {status.ToString()}{eol}content-type: application/octet-stream{eol}{eol}");
                WriteHeader(status, ns);
                WriteBody(path, ns);
                Disconnect(ns);
            }
        }
        private void Disconnect(NetworkStream ns) {
            ns.Flush();
            ns.Close();
            ns.Dispose();
        }
        private string GetPath(string request) {

            string[] lines = request.Split(eol);
            if (lines.Length < 1)
            {
                return docroot+indexFile;
            }
            string[] tokens = lines[0].Split(" ");
            if(tokens.Length < 1)
            {
                return docroot + indexFile;
            }
            string path = tokens[1].Trim();
            Console.WriteLine($"path: {path}");
            return docroot + path;
        }
        public void WriteHeader(short status, NetworkStream ns) {
            byte[] header = Encoding.UTF8.GetBytes($"HTTP/1.1 {status.ToString()}{eol}content-type: application/octet-stream{eol}{eol}");
            ns.Write(header, 0, header.Length);
        }
        public  void WriteBody(string path,  NetworkStream ns) {
            if(!File.Exists(path) ){
                return ;
            }
            Int64 offset = 0;
            FileStream fs = File.Open(path, FileMode.Open);
            Int64 fileSize = fs.Length;
            while(fileSize > offset)
            {
                fs.Seek(offset, SeekOrigin.Begin);
                Int32 currentBufferSize = (fileSize - offset) >= bufferSize ? bufferSize : (Int32)(fileSize - offset);
                byte[] buff = new byte[currentBufferSize];
                fs.Read(buff, 0, currentBufferSize);
                ns.Write(buff);
                ns.Flush();
                offset += currentBufferSize;

            }
            fs.Close();
            fs.Dispose();

            return ;
            
        }  
    }
}
