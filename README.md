# Hệ thống thư viện và đặt mượn sách phân tán

Bài tập lớn học phần **Phát triển ứng dụng phân tán** — Đề tài 20.

## Kiến trúc

```
Client (ASP.NET Core MVC) -> Library API (Minimal API) -> RabbitMQ -> Reservation Worker -> PostgreSQL
                                  |                                        |
                                  +--------------> PostgreSQL <------------+
                                  |
                            Audit/Statistics
```

## Công nghệ

- C# / .NET 10
- ASP.NET Core Minimal API (backend), ASP.NET Core MVC (client)
- PostgreSQL 16 + EF Core 10
- RabbitMQ 3.13 + `RabbitMQ.Client` 7.x
- Outbox pattern, idempotent consumer, correlation ID, health check, Serilog, retry/DLQ
- Docker + Docker Compose

## Cấu trúc

```
src/
  Library.Contracts/    DTO, event, enum dùng chung
  Library.Data/         Entity, DbContext, migration
  Library.Api/          Minimal API + Outbox publisher
  Library.Worker/       Consumer xử lý đặt mượn
  Library.Audit/        Consumer audit/thống kê
  Library.Client.Web/   ASP.NET Core MVC client
tests/Library.Tests/    Unit test
docs/                   Tài liệu, đề bài
docker-compose.infra.yml  PostgreSQL + RabbitMQ (dev)
docker-compose.yml        Full stack (demo)
```

## Yêu cầu môi trường

- .NET SDK 10
- Docker + Docker Compose v2 (chạy trong VM Ubuntu hoặc Docker Desktop)
- Visual Studio (workload ASP.NET and web development) hoặc VS Code

## Cấu hình

Sao chép `.env.example` thành `.env` và điền thông tin:

```bash
cp .env.example .env
```

- Dev (Windows -> VM): đặt `POSTGRES_HOST` / `RABBITMQ_HOST` = IP của VM (chế độ Bridged).
- Demo (trong VM): dùng service name `postgres` / `rabbitmq`.

## Chạy hạ tầng khi dev (trong VM Ubuntu)

```bash
docker compose -f docker-compose.infra.yml up -d
```

Kiểm tra: PostgreSQL `:5432`, RabbitMQ `:5672`, management `http://<VM_IP>:15672`.

Trên Windows kiểm tra kết nối:

```powershell
Test-NetConnection <VM_IP> -Port 5432
Test-NetConnection <VM_IP> -Port 5672
```

## Chạy ứng dụng (dev)

```bash
dotnet run --project src/Library.Api
dotnet run --project src/Library.Worker
dotnet run --project src/Library.Audit
dotnet run --project src/Library.Client.Web
```

Chạy kiểm thử:

```bash
dotnet test
```

## Chạy toàn hệ thống bằng Docker (demo)

```bash
docker compose up -d --build
```

- Client: `http://<VM_IP>:8081`
- API: `http://<VM_IP>:8080`
- Health check: `http://<VM_IP>:8080/health` (live: `/health/live`, ready: `/health/ready`)
- RabbitMQ management: `http://<VM_IP>:15672`
- Khi chạy Docker, API tự động chạy migration (`Database__AutoMigrate=true`).

## API tối thiểu

| Method | Endpoint |
|--------|----------|
| GET | `/books` |
| GET | `/books/{id}` |
| GET | `/users` |
| POST | `/reservations` |
| GET | `/reservations/{id}` |
| GET | `/users/{userId}/reservations` |
| POST | `/reservations/{id}/borrow` |
| POST | `/reservations/{id}/return` |
| GET | `/health` |

## Trạng thái đặt mượn

`Pending -> Reserved -> Borrowed -> Returned` (hoặc `Rejected`)

## Kịch bản demo

1. Trong VM: `docker compose up -d --build` (dev hạ tầng: `docker compose -f docker-compose.infra.yml up -d`).
2. Mở client `http://<VM_IP>:8081`, vào **Sách** → **Chi tiết** → chọn người mượn → **Đặt mượn**.
3. API lưu đơn `Pending` + outbox trong cùng transaction; OutboxPublisher phát `reservation.requested`.
4. Worker khoá bản sách bằng `FOR UPDATE SKIP LOCKED`, cập nhật `Reserved`/`Rejected` và ghi outbox `reservation.processed`.
5. Audit nhận cả hai event và ghi `AuditLogs`; theo dõi đơn ở mục **Đặt mượn**.
6. Đơn `Reserved` → **Xác nhận mượn**; đơn `Borrowed` → **Trả sách**.
7. Kiểm tra `http://<VM_IP>:8080/health`, RabbitMQ management `http://<VM_IP>:15672`, log Serilog của 4 tiến trình.

## Lộ trình

- [x] Tuần 1: Khởi tạo solution, hạ tầng Docker
- [x] Tuần 2: Domain + EF migration + seed
- [x] Tuần 3: Read API + MVC tra cứu
- [x] Tuần 4: Reservation + Outbox
- [x] Tuần 5: Worker + chống oversell + idempotent + retry/DLQ
- [x] Tuần 6: Borrow/Return + Audit + Correlation ID
- [x] Tuần 7: Docker hóa full stack + resilience
- [x] Tuần 8: Test + báo cáo + demo
