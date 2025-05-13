# Coding Standards and Style Guide for C# and React Development

## General Principles

1. **Write clean, maintainable code** - Prioritize readability and maintainability over clever solutions.
2. **Follow the DRY principle** (Don't Repeat Yourself) - Avoid code duplication.
3. **Use consistent naming conventions** - Be consistent within each language ecosystem.
4. **Document your code** - Include comments for complex logic and documentation for public APIs.
5. **Implement proper error handling** - Never silently fail; handle exceptions appropriately.

## C# Standards

### Naming Conventions
- Use PascalCase for class names, method names, and public properties
- Use camelCase for local variables and parameters
- Use _camelCase for private fields
- Use UPPER_CASE for constants
- Prefix interfaces with "I" (e.g., IRepository)

### Code Organization
- One class per file (with filename matching class name)
- Organize namespaces to reflect project structure
- Group related functionality into appropriate namespaces
- Follow standard regions if used by the project (fields, properties, constructors, methods, etc.)

### Coding Practices
- Only TypeScript
- Use properties instead of public fields
- Make classes sealed when not designed for inheritance
- Use C# 8.0+ features when appropriate (nullable reference types, pattern matching)
- Use async/await for asynchronous operations instead of callbacks
- Use LINQ for collection operations when appropriate
- Prefer dependency injection for managing dependencies

## React Standards

### Component Structure
- Use functional components with hooks over class components
- Keep components small and focused on a single responsibility
- Separate UI components from logic/container components
- Use named exports for components

### Naming Conventions
- Use PascalCase for component names
- Use camelCase for variables, props, and function names
- Use kebab-case for file names
- Use descriptive names for files and components

### State Management
- Use React Context API and hooks for state that needs to be shared
- Keep state as local as possible to where it's used
- Use appropriate hooks (useState, useEffect, useContext, etc.)
- Avoid prop drilling by using context or state management libraries

### Styling
- Use CSS-in-JS or CSS modules to avoid style conflicts
- Follow a consistent approach to styling throughout the project
- Implement responsive design principles
- Use semantic HTML elements

## Problem-Solving Approach

1. **Ask questions before implementing** - Ensure you understand the requirements fully
2. **Don't create workarounds or mockups** - If you encounter an issue that seems to require a workaround, ask for clarification
3. **Follow established patterns** - Use design patterns appropriate for the language and problem
4. **Be explicit rather than implicit** - Make behavior obvious through code
5. **Prioritize correctness over performance** until profiling identifies bottlenecks

## Code Quality and Consistency

1. **Follow established linting rules** - Use the project's ESLint and/or C# analyzers
2. **Write tests** - Implement unit tests for logic and integration tests where appropriate
3. **Document public APIs** - Use XML documentation in C# and JSDoc in JavaScript
4. **Use consistent formatting** - Follow the project's .editorconfig or formatting guidelines
5. **Perform code reviews** - Ensure code adheres to these standards before submission

## When to Ask for Help

1. If requirements are unclear or contradictory
2. When encountering technical limitations that would require workarounds
3. When multiple valid approaches exist and guidance is needed
4. If you're unsure about architectural decisions that might impact the project long-term
5. When dealing with security or performance-critical code

Would you like me to expand on any specific section or add additional guidance?