# Naglfar Architecture Diagrams

This folder contains all Mermaid diagrams used in the Naglfar Analytics documentation.

## Diagram Files

| File | Description | Type |
|------|-------------|------|
| `01-system-architecture.mmd` | Complete system architecture | Graph |
| `02-authentication-flow.mmd` | Authentication flow (e-token → auth-token) | Sequence |
| `03-request-flow-allowed.mmd` | Request flow for allowed traffic | Sequence |
| `04-request-flow-blocked-ip.mmd` | Request flow for blocked IP | Sequence |
| `05-request-flow-blocked-token.mmd` | Request flow for compromised token | Sequence |
| `06-data-pipeline.mmd` | 4-stage data analytics pipeline | Graph |
| `07-architecture-components.mmd` | System components (architecture-beta) | Architecture |
| `08-architecture-dataflow.mmd` | Data flow architecture (architecture-beta) | Architecture |
| `09-architecture-interactions.mmd` | Component interactions (architecture-beta) | Architecture |

## Setup

### Prerequisites

**Docker (Required)**

This project uses Docker to generate diagrams - no npm installation needed!

```bash
# Verify Docker is installed
docker --version
```

### Verify Setup
```bash
make diagrams-check
```

This will confirm Docker is available and show the mermaid-cli version (11.12.0).

## Usage

### Generate All Diagrams
```bash
make diagrams
```

This will:
- Read all `.mmd` files in this folder
- Generate corresponding `.svg` files
- Use transparent background and neutral theme

### Validate Diagram Syntax
```bash
make diagrams-validate
```

This will:
- Render each `.mmd` file and capture the output
- Check for syntax error patterns in the output
- Display error details if syntax errors are found
- Exit with error code if any diagram is invalid

**How it works**: Renders to stdout and searches for error patterns like "Syntax error", "Error in graph", or "Parse error".

### Clean Generated Files
```bash
make diagrams-clean
```

This will:
- Remove all `.svg` files from this folder
- Keep `.mmd` source files intact

### Generate Single Diagram

```bash
docker run --rm -v $(pwd):/data minlag/mermaid-cli:11.12.0 \
  -i /data/01-system-architecture.mmd \
  -o /data/01-system-architecture.svg \
  -b transparent -t neutral
```

Or use the Makefile pattern which handles paths automatically:
```bash
make docs/assets/diagrams/01-system-architecture.svg
```

## Editing Diagrams

1. Edit the `.mmd` source file
2. Regenerate SVG: `make diagrams`
3. Commit both `.mmd` and `.svg` files to git

## Themes and Styling

Current settings:
- **Background**: Transparent (`-b transparent`)
- **Theme**: Neutral (`-t neutral`)

Available themes:
- `default` - Default Mermaid theme
- `neutral` - Neutral theme (current)
- `dark` - Dark theme
- `forest` - Forest theme

To change theme, edit the Makefile diagram generation command.

## Troubleshooting

### Error: Docker not found

**Solution**: Install Docker
- **macOS**: Download from https://www.docker.com/products/docker-desktop
- **Linux**: `sudo apt-get install docker.io` or use your package manager
- **Windows**: Download from https://www.docker.com/products/docker-desktop

### Permission denied when running Docker

**Linux users** may need to add themselves to the docker group:
```bash
sudo usermod -aG docker $USER
# Then log out and log back in
```

### Diagram generation is slow

First run pulls the Docker image (minlag/mermaid-cli:11.12.0), which may take a minute. Subsequent runs are much faster as the image is cached locally.

To pre-pull the image:
```bash
docker pull minlag/mermaid-cli:11.12.0
```

### Diagram Not Rendering

1. Validate syntax: `make diagrams-validate`
2. Check Mermaid documentation: https://mermaid.js.org/
3. Test in Mermaid Live Editor: https://mermaid.live/

## CI/CD Integration

### GitHub Actions

```yaml
- name: Generate diagrams
  run: make diagrams
```

That's it! No additional setup needed - Docker is available in GitHub Actions by default.

### GitLab CI

```yaml
diagrams:
  image: docker:latest
  services:
    - docker:dind
  script:
    - make diagrams
```

### Using mermaid-cli image directly

```yaml
diagrams:
  image: minlag/mermaid-cli:11.12.0
  script:
    - cd docs/assets/diagrams
    - for file in *.mmd; do mmdc -i "$file" -o "${file%.mmd}.svg" -b transparent -t neutral; done
```

## References

- [Mermaid Documentation](https://mermaid.js.org/)
- [Mermaid CLI](https://github.com/mermaid-js/mermaid-cli)
- [Mermaid Live Editor](https://mermaid.live/)
- [Architecture Diagrams (Beta)](https://mermaid.js.org/syntax/architecture.html)
