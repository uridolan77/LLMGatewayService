# LLM Gateway Microservice

A robust, production-ready API gateway for multiple Large Language Model providers with extensive configuration options and reliable fallback mechanisms.

## Features

- **Multi-Provider Support**: Seamlessly integrate with OpenAI, Anthropic, Cohere, and HuggingFace
- **Unified API**: Single API for completions and embeddings across all providers
- **Intelligent Routing**: Route requests to the appropriate model based on mappings or infer the provider from the model ID
- **Fallback Mechanisms**: Automatically retry with alternative models when primary models fail
- **Caching**: Configurable caching support with Redis or in-memory storage
- **Token Usage Tracking**: Monitor token usage across providers, models, and users
- **Rate Limiting**: Configurable rate limiting for API endpoints
- **Authentication & Authorization**: JWT authentication and API key validation with role-based access control
- **Comprehensive Logging**: Structured logging with Serilog
- **Telemetry**: Optional Application Insights integration
- **Extensible**: Easily add new providers through the abstract provider interface
- **Containerization**: Ready-to-use Docker and docker-compose configuration

## Architecture

The LLM Gateway follows a clean, layered architecture:

- **API Layer**: Controllers, middleware, and API-specific models
- **Core Layer**: Business logic, interfaces, and domain models
- **Providers Layer**: Implementation of specific LLM providers
- **Infrastructure Layer**: Cross-cutting concerns like caching, logging, and telemetry

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker and docker-compose (for containerized deployment)
- API keys for the LLM providers you plan to use

### Configuration

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/llm-gateway.git
   cd llm-gateway
   ```

2. Update the API keys in `appsettings.json` or use environment variables:
   ```json
   "Providers": {
     "OpenAI": {
       "ApiKey": "your-openai-api-key"
     },
     "Anthropic": {
       "ApiKey": "your-anthropic-api-key"
     }
   }
   ```

3. Configure API keys for gateway access:
   ```json
   "ApiKeys": {
     "ApiKeys": [
       {
         "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
         "Key": "your-secure-api-key",
         "Owner": "Your Name",
         "Permissions": ["completion", "embedding", "admin"]
       }
     ]
   }
   ```

### Running Locally

```bash
cd src/LLMGateway.API
dotnet run
```

The API will be available at `https://localhost:7241` and `http://localhost:5241`.

### Docker Deployment

1. Create a `.env` file with your API keys:
   ```
   OPENAI_API_KEY=your-openai-api-key
   ANTHROPIC_API_KEY=your-anthropic-api-key
   COHERE_API_KEY=your-cohere-api-key
   HUGGINGFACE_API_KEY=your-huggingface-api-key
   JWT_SECRET=your-secure-jwt-secret
   API_KEY=your-gateway-api-key
   ```

2. Build and run the containers:
   ```bash
   docker-compose up -d
   ```

The API will be available at `http://localhost:5000` and `https://localhost:5001`.

## API Usage

### Authentication

All API requests require an API key provided in the header:

```
X-API-Key: your-api-key
```

### Completions

```http
POST /api/v1/completion
Content-Type: application/json
X-API-Key: your-api-key

{
  "model": "openai.gpt-4-turbo",
  "messages": [
    {"role": "system", "content": "You are a helpful assistant."},
    {"role": "user", "content": "Tell me about microservices architecture."}
  ],
  "temperature": 0.7,
  "max_tokens": 1000
}
```

### Streaming Completions

```http
POST /api/v1/completion/stream
Content-Type: application/json
X-API-Key: your-api-key

{
  "model": "anthropic.claude-3-opus",
  "messages": [
    {"role": "user", "content": "Write a creative story about a robot learning to paint."}
  ],
  "temperature": 0.9,
  "max_tokens": 2000
}
```

### Embeddings

```http
POST /api/v1/embedding
Content-Type: application/json
X-API-Key: your-api-key

{
  "model": "openai.text-embedding-ada-002",
  "input": "The food was delicious and the service was excellent."
}
```

### Available Models

```http
GET /api/v1/models
X-API-Key: your-api-key
```

### Model Details

```http
GET /api/v1/models/openai.gpt-4-turbo
X-API-Key: your-api-key
```

### Models by Provider

```http
GET /api/v1/models/provider/OpenAI
X-API-Key: your-api-key
```

### Admin Endpoints

```http
GET /api/v1/admin/usage?startDate=2023-01-01&endDate=2023-01-31
X-API-Key: your-api-key
```

## Advanced Configuration

### Model Mappings

Configure model aliases and routing in the `LLMRouting` section:

```json
"LLMRouting": {
  "ModelMappings": [
    {
      "ModelId": "gpt-4",
      "ProviderName": "OpenAI",
      "ProviderModelId": "gpt-4-0125-preview",
      "DisplayName": "GPT-4 Turbo",
      "ContextWindow": 128000
    }
  ]
}
```

### Fallback Configuration

Configure fallback behavior for specific models:

```json
"Fallbacks": {
  "EnableFallbacks": true,
  "Rules": [
    {
      "ModelId": "openai.gpt-4-turbo",
      "FallbackModels": ["openai.gpt-3.5-turbo", "anthropic.claude-3-sonnet"],
      "ErrorCodes": ["rate_limit_exceeded", "server_error"]
    }
  ]
}
```

### Redis Configuration

Configure Redis for distributed caching:

```json
"Redis": {
  "ConnectionString": "your-redis-server:6379",
  "InstanceName": "LLMGateway:"
}
```

## Extensibility

### Adding a New Provider

1. Create a new provider class that implements `ILLMProvider`
2. Add provider-specific options and models
3. Register the provider in the DI container in `ServiceCollectionExtensions.cs`
4. Add provider configuration in `appsettings.json`

## Monitoring and Management

- Health checks available at `/health`
- Swagger UI available at `/` (in Development environment)
- Token usage statistics at `/api/v1/admin/usage`

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgements

- Built with ASP.NET Core and .NET 8
- Uses Redis for distributed caching
- Uses Serilog for structured logging
- Uses Polly for resilience and transient fault handling
