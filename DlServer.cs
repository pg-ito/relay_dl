using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace relay_dl
{
    class DlServer
    {
        private TcpListener tcp;
        private IPAddress ip = IPAddress.Any;
        private int port = 9821;
        private const string eol = "\r\n";
        private const string indexFile = "/index.html";
        Int32 bufferSize = 4096;

        public string Docroot { get ; set ; } = "docroot";

        public DlServer(int portConfig=9821) {
            tcp = new TcpListener(ip , portConfig);
        }
        public async Task Listen()
        {
            tcp.Start();
            Console.WriteLine($"Docroot {Docroot}, Listening {ip}:{port} ...");

            while (true)
            {
                TcpClient client = await tcp.AcceptTcpClientAsync();
                WaitForRequest(client);
            }
        }
        private async void WaitForRequest(TcpClient client) {

            NetworkStream ns = client.GetStream();

            while (!ns.DataAvailable) { };// @TODO Request Timeout

            Byte[] bytes = new Byte[client.Available];

            ns.Read(bytes, 0, bytes.Length);
            String data = Encoding.UTF8.GetString(bytes);

            if (!Regex.IsMatch(data, "^GET"))
            {
                Disconnect(ns);
                return;
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
                return;
            }

            byte[] header = Encoding.UTF8.GetBytes($"HTTP/1.1 {status.ToString()}{eol}content-type: application/octet-stream{eol}{eol}");
            WriteHeader(status, ns);
            await WriteBody(path, ns);
            Disconnect(ns);
            
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
                return Docroot+indexFile;
            }
            string[] tokens = lines[0].Split(" ");
            if(tokens.Length < 1)
            {
                return Docroot + indexFile;
            }
            string path = tokens[1].Trim();
            Console.WriteLine($"path: {path}");
            return Docroot + path;
        }
        public void WriteHeader(short status, NetworkStream ns) {
            byte[] header = Encoding.UTF8.GetBytes($"HTTP/1.1 {status.ToString()}{eol}content-type: application/octet-stream{eol}{eol}");
            ns.Write(header, 0, header.Length);
        }
        public async Task WriteBody(string path,  NetworkStream ns) {
            if(!File.Exists(path) ){
                return ;
            }
            Int64 offset = 0;
            FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read  );
            Int64 fileSize = fs.Length;
            while(fileSize > offset)
            {
                fs.Seek(offset, SeekOrigin.Begin);
                Int32 currentBufferSize = (fileSize - offset) >= bufferSize ? bufferSize : (Int32)(fileSize - offset);
                byte[] buff = new byte[currentBufferSize];
                await fs.ReadAsync(buff, 0, currentBufferSize);
                await ns.WriteAsync(buff);
                await ns.FlushAsync();
                offset += currentBufferSize;

            }
            fs.Close();
            await fs.DisposeAsync();

            return ;
            
        }  
    }
}
