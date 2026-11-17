# Segurança Elétrica em Casa

Jogo educativo 2D (top-down, pixel art) sobre segurança no uso de eletricidade em casa,
desenvolvido como projeto extensionista da disciplina **Programação de Computadores**
(2º semestre de 2026) — Centro Universitário Sagrado Coração (UNISAGRADO).

O jogador explora os cômodos de uma casa (Quarto, Banheiro, Sala e Cozinha) e precisa
identificar e corrigir 5 perigos elétricos em cada um, respondendo a um quiz de
múltipla escolha sobre o que fazer em cada situação.

## Tecnologias

- C#
- .NET Framework 4.8.1
- Windows Forms (sem engine de jogos — todo o game loop e a renderização foram
  implementados manualmente com `System.Windows.Forms` e `System.Drawing`/GDI+)

## Como rodar

Abra `SegurancaEletricaEmCasa.slnx` no Visual Studio 2022 e execute (F5), ou compile e
rode o `.exe` gerado em `bin/Debug`.

## Estrutura do projeto

- `Forms/` — telas da aplicação (menu inicial, jogo, aprender, créditos)
- `Game/` — regras da partida (vidas, estado, cômodos)
- `Entities/` — personagem e perigos elétricos
- `Quiz/` — lógica das perguntas e respostas
- `Data/` — dados fixos dos cômodos e dos 20 perigos/perguntas
- `UI/` — componentes visuais reutilizáveis (botões, fontes, overlay do quiz)
