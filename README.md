# 📱 Real Word 

Aplicativo educativo que utiliza **Realidade Aumentada (RA)** e **Inteligência Artificial (IA)** para auxiliar no aprendizado de inglês de forma interativa e contextualizada.

---

## 🎯 Objetivo do Projeto

O **Real Word** é uma ferramenta inovadora que permite ao usuário apontar a câmera do celular para objetos do cotidiano e visualizar instantaneamente:
- Nome do objeto em **inglês** e **português**
- Frases contextuais em inglês
- Pronúncia via **Text-to-Speech (TTS)**
- Histórico de traduções para revisão

Este projeto está alinhado com o **ODS 4** da ONU:
> *"Assegurar a educação inclusiva e equitativa de qualidade, e promover oportunidades de aprendizagem ao longo da vida para todas e todos."*

---

## 🛠️ Tecnologias Utilizadas

- **Unity (C#)** → Desenvolvimento do aplicativo móvel com AR Foundation
- **NestJS** → API backend para gerenciamento de requisições
- **Gemini API (Google AI Studio)** → Geração de frases contextuais em inglês
- **AR Foundation** → Detecção de objetos e realidade aumentada
- **Figma** → Prototipação e design da interface (UI/UX)
- **Android TTS** → Conversão de texto em áudio nativo

---

## 📱 Funcionalidades Principais

### 🔍 Detecção e Tradução de Objetos
- Feed da câmera em tempo real com AR Foundation
- Detecção de objetos usando visão computacional
- Geração de frases contextuais em inglês via Gemini API
- Conversão de texto em áudio (TTS) nativo
- Exibição de traduções sobreposta à imagem da câmera

### 🧭 Interface e Navegação
- Menu lateral com acesso às principais funcionalidades
- Navegação entre telas (Principal, Configurações, Histórico)

### 📚 Histórico e Armazenamento
- Registro local das traduções realizadas
- Exibição do histórico com opção de replay de áudio

### 🔗 Backend e Integração
- Endpoint `/sentences` para geração de frases contextuais

---

## 💾 Sistema de Cache Local

- Implementado em Unity para armazenar nomes e frases geradas pela API Gemini
- Funcionamento em modo offline após primeira consulta
- Economia de recursos reduzindo chamadas desnecessárias à API
- Persistência de dados entre sessões do aplicativo

---

## 📋 Etapas de Desenvolvimento

1. **Pesquisa e Planejamento** → Definição do problema e tecnologias
2. **Prototipação** → Design de interface e experiência no Figma
3. **Configuração do Ambiente** → Unity + NestJS + Integrações
4. **Implementação Core** → Detecção AR + Integração Gemini API
5. **Desenvolvimento de Funcionalidades** → TTS, Histórico, UI
6. **Testes e Validação** → Usabilidade, desempenho e experiência
7. **Entrega e Documentação** → PoC final e documentação completa

---

## 🧪 Testes Realizados

- **Testes de Usabilidade**: Observação de interações reais com usuários
- **Validação de Detecção**: Eficácia no reconhecimento de objetos cotidianos
- **Experiência do Usuário**: Avaliação da fluidez e intuitividade da interface
- **Desempenho Técnico**: Tempo de resposta e estabilidade do aplicativo

---

## 👥 Autores
Desenvolvido por:
- Felipe Augusto de Almeida Mariano - Áudio (TTS) & Testes 
- Felipe Rusig de Paiva - Integração Gemini API / Backend 
- João Rafael Jordão Pereira - Documentação & Pesquisa 
- Kaique Medeiros Govani - Unity Lead & Coordenação 
- Mateus Nauhan Vieira Matos - Detecção de Objetos 
- Milton Rogerio Dotto Penha Junior - UI/UX no Unity 

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo `LICENSE` para mais detalhes.
