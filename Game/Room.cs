using System.Collections.Generic;
using System.Drawing;
using SegurancaEletricaEmCasa.Entities;

namespace SegurancaEletricaEmCasa.Game
{
    //Uma fase do jogo: um cômodo da casa com seu fundo, posição inicial do jogador e perigos.
    //O fundo (BackgroundSprite) mostra os perigos ativos; cada perigo resolvido cola por cima
    //o recorte "consertado" correspondente (ver MainForm.DrawHazard).
    public class Room
    {
        //Nome exibido no HUD e nas mensagens (ex.: "Quarto").
        public string Name { get; }
        public string BackgroundSprite { get; }
        public PointF PlayerStart { get; }
        public List<Hazard> Hazards { get; }

        //Contorno do piso (losango isométrico) em coordenadas da tela: os "pés" do jogador só
        //podem ficar dentro dele, o que impede andar pelas paredes ou fora do cômodo.
        public Point[] FloorPolygon { get; }

        //Frase da mensagem de vitória (ex.: "Agora o quarto está seguro!").
        public string SafeMessage { get; }

        public Room(string name, string backgroundSprite, PointF playerStart, Point[] floorPolygon,
            List<Hazard> hazards, string safeMessage)
        {
            Name = name;
            BackgroundSprite = backgroundSprite;
            PlayerStart = playerStart;
            FloorPolygon = floorPolygon;
            Hazards = hazards;
            SafeMessage = safeMessage;
        }
    }
}
