using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BoatSim
{
    class BoatDto
    {
        public int Id;
        public string Name;
        public bool ctrlW, ctrlS, ctrlA, ctrlD;
        public Vector3[] VerletPositions;
    }
    internal class Network : IDisposable
    {
        UdpClient socket = new UdpClient(AddressFamily.InterNetwork);
        MemoryStream ms = new MemoryStream();
        public Network()
        {
            socket.Connect(IPAddress.Parse("152.66.189.27"), 8081);
        }

        public void Dispose()
        {
            socket.Close();
        }

        public void Send(BoatDto boatDto)
        {
            ms.SetLength(0);
            using (var w = new BinaryWriter(ms, Encoding.UTF8, true))
            {
                w.Write(boatDto.Name);
                w.Write(boatDto.ctrlW);
                w.Write(boatDto.ctrlA);
                w.Write(boatDto.ctrlS);
                w.Write(boatDto.ctrlD);
                w.Write(boatDto.VerletPositions.Length);
                for (int i = 0; i < boatDto.VerletPositions.Length; i++)
                {
                    w.Write(boatDto.VerletPositions[i].X);
                    w.Write(boatDto.VerletPositions[i].Y);
                    w.Write(boatDto.VerletPositions[i].Z);
                }
            }
            socket.Send(ms.GetBuffer(), (int)ms.Length);
        }

        public List<BoatDto> Receive()
        {
            if (socket.Available < 10)
                return null;
            var from = new IPEndPoint(IPAddress.Any, 0);
            var buffer = socket.Receive(ref from);
            using var r = new BinaryReader(new MemoryStream(buffer));
            var ret = new List<BoatDto>();
            while (r.BaseStream.Position < r.BaseStream.Length)
            {
                var dto = new BoatDto()
                {
                    Id = r.ReadInt32(),
                    Name = r.ReadString(),
                    ctrlW = r.ReadBoolean(),
                    ctrlA = r.ReadBoolean(),
                    ctrlS = r.ReadBoolean(),
                    ctrlD = r.ReadBoolean(),
                    VerletPositions = new Vector3[r.ReadInt32()]
                };
                for (int j = 0; j < dto.VerletPositions.Length; j++)
                {
                    dto.VerletPositions[j].X = r.ReadSingle();
                    dto.VerletPositions[j].Y = r.ReadSingle();
                    dto.VerletPositions[j].Z = r.ReadSingle();
                }
                ret.Add(dto);
            }
            return ret;
        }
    }
}
