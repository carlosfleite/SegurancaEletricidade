using System.Collections.Generic;
using System.Drawing;
using SegurancaEletricaEmCasa.Game;

namespace SegurancaEletricaEmCasa.Data
{
    //Ordem das fases: ao resolver todos os perigos de um cômodo, o jogador segue para o próximo.
    //Os fundos têm 1000x650 (mesmo tamanho da janela) e a posição inicial de cada cômodo fica
    //num trecho de piso livre, longe das áreas que disparam os quizzes.
    //O polígono do piso lista os 4 cantos do chão: esquerdo, fundo, direito e frente.
    public static class RoomFactory
    {
        public static List<Room> CreateRooms()
        {
            return new List<Room>
            {
                new Room("Quarto", "Sprites/quarto.png", new PointF(443, 276),
                    Floor(55, 410, 465, 192, 950, 430, 505, 630),
                    HazardFactory.CreateBedroomHazards(), "Agora o quarto está seguro!"),

                new Room("Banheiro", "Sprites/banheiro.png", new PointF(404, 236),
                    Floor(116, 438, 500, 240, 900, 448, 500, 622),
                    HazardFactory.CreateBathroomHazards(), "Agora o banheiro está seguro!"),

                new Room("Sala", "Sprites/sala.png", new PointF(194, 286),
                    Floor(100, 422, 540, 235, 897, 440, 500, 626),
                    HazardFactory.CreateLivingRoomHazards(), "Agora a sala está segura!"),

                new Room("Cozinha", "Sprites/cozinha.png", new PointF(484, 276),
                    Floor(105, 445, 470, 245, 900, 450, 500, 628),
                    HazardFactory.CreateKitchenHazards(), "Agora a cozinha está segura!")
            };
        }

        private static Point[] Floor(int leftX, int leftY, int backX, int backY, int rightX, int rightY, int frontX, int frontY)
        {
            return new[]
            {
                new Point(leftX, leftY),
                new Point(backX, backY),
                new Point(rightX, rightY),
                new Point(frontX, frontY)
            };
        }
    }
}
