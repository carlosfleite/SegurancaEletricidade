using System.Collections.Generic;
using System.Drawing;
using SegurancaEletricaEmCasa.Entities;
using SegurancaEletricaEmCasa.Quiz;

namespace SegurancaEletricaEmCasa.Data
{
    //Perigos de cada cômodo. Bounds = área (no chão) que abre o quiz quando o jogador pisa
    //perto; overlayBounds = região onde o recorte "consertado" (Sprites/perigo_resolvido_<id>.png)
    //é colado por cima do fundo depois que o perigo é resolvido.
    public static class HazardFactory
    {
        public static List<Hazard> CreateBedroomHazards()
        {
            return new List<Hazard>
            {
                new Hazard(
                    id: "copo_agua",
                    bounds: new Rectangle(83, 361, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(40, 330, 140, 100),
                    question: new QuizQuestion(
                        prompt: "Perigo! Copo de água perto da tomada. O que fazer?",
                        options: new[]
                        {
                            "Tirar o copo e secar a região",
                            "Deixar lá e tomar cuidado ao passar"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Isso mesmo! Água perto de tomadas pode causar curto-circuito e choque.",
                        wrongFeedback: "Cuidado! Água e eletricidade juntas podem causar choques graves."
                    )),

                new Hazard(
                    id: "carregador_danificado",
                    bounds: new Rectangle(776, 411, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(740, 380, 160, 90),
                    question: new QuizQuestion(
                        prompt: "Ops! Fio desencapado. O que fazer?",
                        options: new[]
                        {
                            "Continuar usando, já que ainda funciona",
                            "Trocar o fio danificado"
                        },
                        correctOptionIndex: 1,
                        correctFeedback: "Correto! Fios desencapados podem causar choques e até incêndios.",
                        wrongFeedback: "Fios danificados nunca devem ser usados: eles oferecem risco de choque e incêndio."
                    )),

                new Hazard(
                    id: "benjamim_sobrecarregado",
                    bounds: new Rectangle(181, 336, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(60, 230, 200, 120),
                    question: new QuizQuestion(
                        prompt: "Muitos aparelhos ligados em um único benjamim (T). O que fazer?",
                        options: new[]
                        {
                            "Distribuir os aparelhos em tomadas diferentes",
                            "Deixar todos ligados, pois um benjamim aguenta"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Ótimo! Sobrecarregar uma tomada pode superaquecer os fios e causar incêndios.",
                        wrongFeedback: "Sobrecarga elétrica é uma das principais causas de incêndios domésticos."
                    )),

                new Hazard(
                    id: "aparelho_coberto",
                    bounds: new Rectangle(573, 309, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(500, 270, 200, 120),
                    question: new QuizQuestion(
                        prompt: "Há roupas cobrindo um aparelho elétrico ligado perto da tomada. O que fazer?",
                        options: new[]
                        {
                            "Deixar coberto, pois esquenta mais rápido assim",
                            "Retirar as roupas de cima e manter o aparelho ventilado"
                        },
                        correctOptionIndex: 1,
                        correctFeedback: "Isso! Aparelhos cobertos podem superaquecer e provocar incêndios.",
                        wrongFeedback: "Cobrir aparelhos elétricos ligados impede a ventilação e pode causar incêndios."
                    )),

                new Hazard(
                    id: "fio_sob_tapete",
                    bounds: new Rectangle(359, 536, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(320, 495, 360, 115),
                    question: new QuizQuestion(
                        prompt: "Um fio elétrico passa embaixo do tapete. O que fazer?",
                        options: new[]
                        {
                            "Retirar o fio de baixo do tapete e organizá-lo com canaletas ou protetores",
                            "Deixar assim, pois fica mais bonito sem fios aparentes"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Perfeito! Fios sob tapetes podem se romper e superaquecer sem serem percebidos.",
                        wrongFeedback: "Fios escondidos sob tapetes correm risco de dano e superaquecimento sem serem notados a tempo."
                    ))
            };
        }

        public static List<Hazard> CreateBathroomHazards()
        {
            return new List<Hazard>
            {
                new Hazard(
                    id: "secador_pia",
                    bounds: new Rectangle(276, 441, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(168, 248, 194, 197),
                    question: new QuizQuestion(
                        prompt: "Um secador de cabelo ligado está apoiado na beira da pia cheia de água. O que fazer?",
                        options: new[]
                        {
                            "Deixar ali, é só não esbarrar nele",
                            "Desligar da tomada e guardar longe da água"
                        },
                        correctOptionIndex: 1,
                        correctFeedback: "Isso! Se o secador cair na água ligado, pode causar um choque fatal.",
                        wrongFeedback: "Aparelhos ligados perto de água podem cair e eletrificar a água. Nunca arrisque!"
                    )),

                new Hazard(
                    id: "tomada_queimada",
                    bounds: new Rectangle(126, 411, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(112, 350, 56, 65),
                    question: new QuizQuestion(
                        prompt: "A tomada está com marcas de queimado em volta. O que fazer?",
                        options: new[]
                        {
                            "Parar de usar e chamar um eletricista para trocá-la",
                            "Continuar usando, a mancha é só sujeira"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Correto! Marcas de queimado indicam superaquecimento ou mau contato.",
                        wrongFeedback: "Tomadas escurecidas estão superaquecendo e podem pegar fogo a qualquer momento."
                    )),

                new Hazard(
                    id: "chuveiro_fiacao",
                    bounds: new Rectangle(666, 306, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(640, 68, 122, 67),
                    question: new QuizQuestion(
                        prompt: "Os fios do chuveiro elétrico estão expostos e soltando faíscas. O que fazer?",
                        options: new[]
                        {
                            "Tomar banho rapidinho e resolver depois",
                            "Desligar o disjuntor e chamar um eletricista"
                        },
                        correctOptionIndex: 1,
                        correctFeedback: "Muito bem! Fiação exposta no chuveiro, com água por perto, é risco grave de choque.",
                        wrongFeedback: "Nunca use um chuveiro com fios expostos: água e eletricidade juntas causam choques graves."
                    )),

                new Hazard(
                    id: "tomada_box",
                    bounds: new Rectangle(541, 341, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(545, 208, 105, 184),
                    question: new QuizQuestion(
                        prompt: "Há uma tomada dentro do box, e a água está escorrendo por ela até o chão. O que fazer?",
                        options: new[]
                        {
                            "Secar tudo e mudar a tomada para longe da água",
                            "Ignorar, a água seca sozinha"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Isso mesmo! Tomadas no banheiro devem ficar longe do chuveiro e da banheira.",
                        wrongFeedback: "Água escorrendo por uma tomada pode causar curto-circuito e choque em quem pisar ali."
                    )),

                new Hazard(
                    id: "aparelho_banheira",
                    bounds: new Rectangle(746, 421, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(722, 332, 163, 86),
                    question: new QuizQuestion(
                        prompt: "Uma chapinha ligada na tomada caiu dentro da banheira. O que fazer?",
                        options: new[]
                        {
                            "Enfiar a mão na água para pegar a chapinha",
                            "Desligar o disjuntor antes de tocar na água ou no aparelho"
                        },
                        correctOptionIndex: 1,
                        correctFeedback: "Perfeito! Primeiro corte a energia; só depois retire o aparelho da água.",
                        wrongFeedback: "Tocar na água com um aparelho ligado dentro dela pode causar um choque fatal!"
                    ))
            };
        }

        public static List<Hazard> CreateLivingRoomHazards()
        {
            return new List<Hazard>
            {
                new Hazard(
                    id: "tomada_sem_espelho",
                    bounds: new Rectangle(531, 266, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(515, 195, 77, 87),
                    question: new QuizQuestion(
                        prompt: "A tampa (espelho) da tomada caiu e os fios estão à mostra. O que fazer?",
                        options: new[]
                        {
                            "Desligar o disjuntor e recolocar a tampa com os fios isolados",
                            "Deixar assim, ninguém mexe ali mesmo"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Correto! Fios expostos na parede podem dar choque em quem encostar, principalmente crianças.",
                        wrongFeedback: "Tomadas sem tampa deixam partes energizadas expostas: um toque já basta para levar choque."
                    )),

                new Hazard(
                    id: "fios_emaranhados",
                    bounds: new Rectangle(616, 396, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(555, 283, 245, 219),
                    question: new QuizQuestion(
                        prompt: "Vários fios estão embolados e espalhados pelo chão da sala. O que fazer?",
                        options: new[]
                        {
                            "Pular por cima quando passar",
                            "Organizar os fios junto à parede, com presilhas ou canaletas"
                        },
                        correctOptionIndex: 1,
                        correctFeedback: "Ótimo! Fios soltos no chão causam tropeços e se danificam ao serem pisados.",
                        wrongFeedback: "Fios pisados e embolados se desgastam, esquentam e podem causar acidentes e incêndios."
                    )),

                new Hazard(
                    id: "tomada_sobrecarregada",
                    bounds: new Rectangle(806, 411, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(800, 312, 80, 116),
                    question: new QuizQuestion(
                        prompt: "TV, videogame, som e mais aparelhos estão ligados em adaptadores na mesma tomada. O que fazer?",
                        options: new[]
                        {
                            "Ligar ainda mais um adaptador para caber tudo",
                            "Usar um filtro de linha de qualidade e dividir entre tomadas"
                        },
                        correctOptionIndex: 1,
                        correctFeedback: "Isso! Adaptadores empilhados sobrecarregam a tomada e podem derreter.",
                        wrongFeedback: "Benjamins e adaptadores em cascata superaquecem a tomada e são causa comum de incêndios."
                    )),

                new Hazard(
                    id: "fio_lampada",
                    bounds: new Rectangle(586, 515, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(560, 502, 92, 70),
                    question: new QuizQuestion(
                        prompt: "O fio do abajur está rompido e soltando faíscas perto do tapete. O que fazer?",
                        options: new[]
                        {
                            "Desligar o abajur da tomada e trocar o fio",
                            "Passar fita adesiva comum e continuar usando"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Muito bem! Fio rompido deve ser trocado, ainda mais perto de tecidos que pegam fogo.",
                        wrongFeedback: "Fita comum não isola direito: o fio continua esquentando e pode incendiar o tapete."
                    ))
            };
        }

        public static List<Hazard> CreateKitchenHazards()
        {
            return new List<Hazard>
            {
                new Hazard(
                    id: "bancada_sobrecarregada",
                    bounds: new Rectangle(456, 326, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(408, 140, 194, 152),
                    question: new QuizQuestion(
                        prompt: "Torradeira, liquidificador e cafeteira estão no mesmo benjamim, com fios perto da pia. O que fazer?",
                        options: new[]
                        {
                            "Ligar cada aparelho em uma tomada, longe da pia, só quando for usar",
                            "Deixar tudo ligado no benjamim para ganhar tempo"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Correto! Aparelhos que esquentam puxam muita energia e não devem dividir a mesma tomada.",
                        wrongFeedback: "Vários aparelhos potentes no mesmo benjamim superaquecem os fios, e a água da pia piora tudo."
                    )),

                new Hazard(
                    id: "fio_fogao",
                    bounds: new Rectangle(596, 381, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(640, 222, 88, 134),
                    question: new QuizQuestion(
                        prompt: "O fio da chaleira elétrica está passando por cima do fogão aceso. O que fazer?",
                        options: new[]
                        {
                            "Deixar, o fio é resistente ao calor",
                            "Afastar o fio do fogo e das panelas quentes"
                        },
                        correctOptionIndex: 1,
                        correctFeedback: "Isso! O calor derrete a capa do fio, deixando o cobre exposto.",
                        wrongFeedback: "Fios perto do fogo derretem e podem causar curto-circuito, choque ou incêndio."
                    )),

                new Hazard(
                    id: "fio_geladeira",
                    bounds: new Rectangle(746, 431, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(738, 383, 160, 99),
                    question: new QuizQuestion(
                        prompt: "O fio da geladeira está esmagado atrás dela e soltando faíscas. O que fazer?",
                        options: new[]
                        {
                            "Desligar a geladeira e trocar o fio danificado",
                            "Empurrar a geladeira mais para trás para esconder o fio"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Perfeito! Fio esmagado perde o isolamento e pode dar choque ou pegar fogo.",
                        wrongFeedback: "Esconder o problema não resolve: o fio danificado continua soltando faíscas."
                    )),

                new Hazard(
                    id: "fios_no_chao",
                    bounds: new Rectangle(476, 541, 48, 48),
                    triggerPadding: 16,
                    overlayBounds: new Rectangle(372, 512, 256, 116),
                    question: new QuizQuestion(
                        prompt: "Extensões e fios estão jogados no meio do chão da cozinha. O que fazer?",
                        options: new[]
                        {
                            "Enrolar e guardar as extensões que não estão em uso",
                            "Deixar no chão, assim ficam à mão quando precisar"
                        },
                        correctOptionIndex: 0,
                        correctFeedback: "Ótimo! Fios no chão da cozinha podem ser molhados, pisados ou causar quedas.",
                        wrongFeedback: "Na cozinha o chão costuma ficar molhado: fios largados ali são risco de choque e tropeço."
                    ))
            };
        }
    }
}
