# 🎧 CrateAgent

Organizador de bibliotecas de música para DJs, feito em C# / .NET.
O projeto nasceu de um problema real: uma pasta de downloads do Bandcamp e de outras fontes, cheia de arquivos sem tags e com nomes sujos.

> 🚧 **Em construção.** A base de organização está pronta e testada. Os agentes de IA (Ollama) e o curador de sets são as próximas fases.

## O que já funciona

- ✅ **Scanner** que lê as tags dos arquivos de áudio (TagLibSharp)
- ✅ **Fallback pelo nome do arquivo** quando as tags estão vazias (suporta hífen, meia-risca e travessão)
- ✅ **Limpeza de nomes**, como a remoção da palavra "Premiere" e a normalização de Unicode
- ✅ **Marcação de faixas para revisão**: artista ausente, código de catálogo ou título suspeito
- ✅ **Dry-run**: mostra para onde cada arquivo iria, sem mover nada
- ✅ **Mover de verdade** com log em JSON e **desfazer**
- ✅ 26 testes automatizados com xUnit

### Testes passando
![Testes passando](docs/images/testes.png)

## Como funciona

```
Pasta de músicas ─► Scanner ─► Limpeza ─► Revisão ─► Plano (dry-run) ─► Mover (com log)
                                                                              │
                                                              Desfazer ◄──────┘
```

Faixas com dados confiáveis vão para `Artista/Artista - Título.mp3`.
Faixas duvidosas vão para `_Revisar`, mantendo o nome original.

## Decisões de projeto

- **O código só move o que tem certeza.** O resto vai para revisão, e a IA entrará apenas como sugestão.
- **Ausência de informação não é um dado falso.** Por isso o BPM é `int?`, e não `int` com valor 0.
- **Nunca sobrescreve arquivos.** Se o destino já existe, a faixa é ignorada.
- **Tudo é reversível.** Cada movimentação é registrada em um log que permite desfazer.

## Aprendizados

- Um nome com "Ü" passava no teste e falhava com dados reais, porque o Unicode tem duas formas de representar o mesmo caractere. A correção foi normalizar o texto, com um teste para o bug não voltar.
- Regras que apagam siglas curtas destruiriam artistas reais (`SAN`, `GIO`, `PYLOT`). Nesses casos a regra só sinaliza.

## Stack

C# · .NET 8 · xUnit · TagLibSharp · System.Text.Json

## Estrutura

```
CrateAgent.Core    → modelos e serviços (a lógica)
CrateAgent.Cli     → interface de console
CrateAgent.Tests   → testes automatizados
```

## Como rodar

```bash
git clone https://github.com/AlessandraBatistaJ/crate-agent.git
cd crate-agent
dotnet run --project CrateAgent.Cli
```

> ⚠️ Teste sempre em uma **cópia** da sua pasta de músicas.

## Roadmap

- [x] Scanner, limpeza e revisão
- [x] Dry-run, mover e desfazer
- [ ] Persistência com SQLite
- [ ] Agente com IA local (Ollama) para sugerir artista e título
- [ ] Detecção de duplicatas
- [ ] Detecção de BPM e tom
- [ ] Curador de sets por BPM e tom harmônico
- [ ] Executável publicado em Releases

## Licença

MIT