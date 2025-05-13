# LLM Gateway Microservice

A comprehensive microservice that acts as an intelligent gateway for multiple LLM providers, offering extensive configuration options and a robust API. This solution enables developers to build AI applications that can leverage various LLM providers through a unified interface.

## Key Features

1. **Multi-Provider Integration**
   * Support for OpenAI, Anthropic, Cohere, and HuggingFace
   * Extensible design for adding more providers

2. **Unified API**
   * Consistent endpoints for completions and embeddings
   * Standardized response formats

3. **Advanced Configuration Options**
   * Model mappings for provider routing
   * Fallback mechanisms for error handling
   * Caching with Redis or in-memory storage
   * Token usage tracking and rate limiting

4. **Security**
   * API key authentication
   * JWT token support
   * Role-based access control

5. **Production-Ready Infrastructure**
   * Comprehensive logging with Serilog
   * Health checks and telemetry
   * Docker and docker-compose deployment options
   * Redis integration for distributed scenarios

6. **Advanced Routing Algorithms**
   * Cost-optimized routing
   * Latency-optimized routing
   * Quality-optimized routing
   * Load-balanced routing
   * Content-based routing
   * User-preference routing

7. **Persistent Storage**
   * Database integration with Entity Framework Core
   * Support for SQL Server, PostgreSQL, and SQLite
   * Repository pattern for data access
   * Automatic migrations

8. **Health Monitoring and Metrics**
   * Provider health monitoring
   * Model performance metrics
   * Alerting system

9. **Background Job System**
   * Scheduled jobs with Quartz.NET
   * Token usage reports
   * Provider health checks
   * Model metrics aggregation
   * Database maintenance
   * Cost reports

10. **Admin Dashboard API**
    * Dashboard summary
    * Token usage analysis
    * Provider health status
    * Routing statistics
    * Cost reporting

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose (for containerized deployment)
- SQL Server (optional, for persistent storage)
- Redis (optional, for distributed caching)

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/llm-gateway.git
   cd llm-gateway
   ```

2. Update the API keys in `appsettings.json` or create a `.env` file with the following variables:
   ```
   OPENAI_API_KEY=your-openai-api-key
   ANTHROPIC_API_KEY=your-anthropic-api-key
   COHERE_API_KEY=your-cohere-api-key
   HUGGINGFACE_API_KEY=your-huggingface-api-key
   JWT_SECRET=your-jwt-secret-key
   API_KEY=your-api-key
   DB_PASSWORD=your-database-password
   ```

3. Build the solution:
   ```
   dotnet build
   ```

4. Run the database migrations (if using a database):
   ```
   dotnet ef database update --project src/LLMGateway.Infrastructure --startup-project src/LLMGateway.API
   ```

5. Run locally:
   ```
   dotnet run --project src/LLMGateway.API
   ```

6. Or deploy with Docker Compose:
   ```
   docker-compose up -d
   ```

### Testing the API

Once the service is running, you can test it using curl or any API client:

1. Get available models:
   ```
   curl -X GET "http://localhost:5000/api/v1/models" -H "X-API-Key: your-api-key"
   ```

2. Create a completion:
   ```
   curl -X POST "http://localhost:5000/api/v1/completions" \
     -H "Content-Type: application/json" \
     -H "X-API-Key: your-api-key" \
     -d '{
       "modelId": "openai.gpt-3.5-turbo",
       "messages": [
         {
           "role": "user",
           "content": "Hello, how are you?"
         }
       ]
     }'
   ```

3. Create an embedding:
   ```
   curl -X POST "http://localhost:5000/api/v1/embeddings" \
     -H "Content-Type: application/json" \
     -H "X-API-Key: your-api-key" \
     -d '{
       "modelId": "openai.text-embedding-ada-002",
       "input": "Hello, world!"
     }'
   ```

## Configuration

The service is highly configurable through the `appsettings.json` file or environment variables. Key configuration sections include:

- **Providers**: API keys and endpoints for LLM providers
- **LLMRouting**: Model mappings and routing strategies
- **Fallbacks**: Fallback rules for error handling
- **Persistence**: Database connection and options
- **Monitoring**: Health check configuration
- **BackgroundJobs**: Scheduled job configuration
- **Redis**: Redis connection for distributed scenarios
- **TokenUsage**: Token usage tracking options
- **ApiKeys**: API key configuration for authentication
- **Jwt**: JWT authentication settings

## API Documentation

Once running, API documentation is available at:
- Swagger UI: `http://localhost:5000`

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
