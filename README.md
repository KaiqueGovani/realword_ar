# 📱 Real Word 

Aplicativo educativo que utiliza *Realidade Aumentada (RA)* e *Inteligência Artificial (IA)* para auxiliar no aprendizado de inglês de forma interativa e inclusiva.  

---

## 🎯 Objetivo do Projeto  
O projeto tem como propósito desenvolver uma ferramenta inovadora que permita ao usuário apontar a câmera do celular para objetos do cotidiano e visualizar instantaneamente o nome correspondente em inglês.  

Este projeto está alinhado com o *ODS 4* da ONU:  
> Assegurar a educação inclusiva e equitativa de qualidade, e promover oportunidades de aprendizagem ao longo da vida para todas e todos.  

---

## 🛠️ Tecnologias Utilizadas  
- **Unity (C#)** → Desenvolvimento do front-end e back-end do aplicativo.  
- *NestJS* → Servidor para gerenciamento de requisições e integração com serviços externos.  
- *LLM (Large Language Model)* → Suporte inteligente para reconhecimento e processamento de linguagem.  
- *Figma* → Prototipação e definição do design visual da aplicação.  

---

## 📌 Funcionalidades Principais (MVP)  
- Reconhecimento de objetos por meio da câmera do celular.  
- Identificação instantânea com exibição do nome em inglês.  
- Interface amigável e acessível, com base em protótipos desenvolvidos no Figma.  
- Integração com IA para suporte e evolução futura de funcionalidades.  

---

## 🚀 Etapas do Desenvolvimento  
1. *Prototipação* → Criação do design e fluxo de telas no Figma.  
2. *Configuração do Ambiente* → Unity para mobile + NestJS para servidor.  
3. *Implementação RA + IA* → Reconhecimento de objetos e tradução.  
4. *Integração do Backend* → Comunicação entre o app e o servidor.  
5. *Validação Educacional* → Testes alinhados ao objetivo de aprendizagem inclusiva.  

---

## 🌍 Conexão com os ODS  
Este projeto apoia o *ODS 4 - Educação de Qualidade*, ao oferecer um recurso tecnológico inovador que amplia as oportunidades de aprendizado de línguas de forma prática, interativa e acessível.  

---

## 💾 Sistema de Cache Local  

- Foi implementado um sistema de **cache local** na Unity para armazenar nomes e frases geradas pela LLM, permitindo que o aplicativo funcione mesmo em modo offline.  
- Os dados são armazenados em formato **JSON**, no diretório `Application.persistentDataPath` do dispositivo.  
- Internamente, o cache utiliza um **dicionário em memória (`Dictionary<string, string>`)** para acesso rápido, e sincroniza as informações com o arquivo JSON sempre que novos dados são adicionados.  

---

## ✍️ Autores  
Desenvolvido por:  
- Felipe Mariano  
- Felipe Rusig  
- João Rafael  
- Kaique Govani  
- Mateus Nauhan  
- Milton Penha  

---

## 📖 Licença  
Este projeto está sob a licença MIT. Veja o arquivo `LICENSE` para mais detalhes.
