# Deployment Guide

This guide covers deploying the backend and frontend using Docker and `docker compose`.

1) Prepare environment
- Copy `.env.example` to `.env` and fill production values (DB URL, JWT key, SMTP credentials, storage connection strings).

2) Build images (production)

```powershell
docker compose -f docker-compose.prod.yml build
```

3) Run services

```powershell
docker compose -f docker-compose.prod.yml up -d
```

4) Database migrations
- Run EF migrations from a machine with dotnet and the source code available, or run a migration container step that executes `dotnet ef database update` against the database connection string.

5) TLS / reverse proxy
- Use a reverse-proxy (nginx, traefik) in front of the services. The repo includes `deploy/nginx/prod.conf` — configure TLS certs and proxy rules.

6) Production recommendations
- Use blob storage (S3/Azure Blob) instead of local file storage for document/payslip assets.
- Use a reliable email provider (SendGrid, SES) instead of local outbox.
- PDF generation (payslips, dashboard summary) already uses PDFsharp (`EMS.Infrastructure/Pdf/PdfSharpDocumentService.cs`), MIT licensed with no revenue/company-size restrictions — no license review needed before scaling. It renders with an embedded PT Sans font (`EMS.Infrastructure/Pdf/Fonts/`, SIL OFL 1.1) since PDFsharp 6.x has no OS font access on any platform, including in the Linux Docker image.
- Store `Jwt:Key` and DB credentials in a secrets manager.
- Hash and rotate refresh tokens; do not store plaintext tokens.
- Configure monitoring, logging and healthchecks; set resource limits.

7) Rollback
- Keep a backup of DB and storage assets. Deploy new images with version tags and roll back by redeploying prior tag.
