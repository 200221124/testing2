# iWebITKiosk

iWebITKiosk é uma aplicação simples de Windows que abre automaticamente em FullScreen e não permite sair desse modo. Ele exibe uma página web configurável via um arquivo JSON, ideal para utilizar como KIOSK.

## Funcionalidades

- Modo FullScreen sem bordas
- Sempre no topo
- Mostra apenas uma página web
- O endereço da página pode ser configurado via um arquivo JSON (`config.json`)
- Supressão de combinações de teclas como Alt+F4, Alt+Tab, e as teclas Windows (LWin, RWin)

## Tecnologias Utilizadas

- .NET 9.0
- Windows Forms
- WebView2
- Newtonsoft.Json

## Estrutura do Projeto

- `MainForm.cs`: Lógica principal da aplicação
- `MainForm.Designer.cs`: Inicialização e design do formulário principal
- `Program.cs`: Ponto de entrada da aplicação
- `config.json`: Arquivo de configuração para definir o URL da página a ser exibida
- `logoApp.ico`: Icon da aplicação

