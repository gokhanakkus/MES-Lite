# MES Lite — Textile Production Monitoring & OEE Dashboard

Gerçek bir tekstil fabrikasında kullanılan **MES (Manufacturing Execution System)** mantığını simüle eden, uçtan uca çalışan bir üretim izleme ve **OEE** (Overall Equipment Effectiveness) panosu.

Fabrikaya fiziksel bağlantı olmadan, dahili bir **simülatör** dokuma / boya / kesim makinelerini gerçek zamanlı çalıştırır; üretim, duruş ve kalite verisi üretir; OEE hesaplanır ve **SignalR** ile arayüze canlı yansıtılır.

> Basit bir CRUD değil — gerçek bir mini MES gibi davranır: canlı veri üretimi, OEE hesaplama motoru, gerçek zamanlı olaylar ve kural tabanlı AI bakım önerileri.

---

## Öne Çıkanlar

- **Gerçek zamanlı simülasyon** — `BackgroundService` her 5 saniyede bir üretim üretir, rastgele duruşlar oluşturur, makineleri durdurup başlatır.
- **Fiziksel telemetri & Makine Sağlığı** — her makine için canlı RPM, hız, yük, titreşim, sıcaklık ve aşınma; bunlardan türetilen **0–100 Health Score**. Telemetri hem üretimi hem arıza olasılığını etkiler (predictive-maintenance mantığı).
- **Zaman serisi & RUL** — telemetri `MachineTelemetrySnapshot` tablosunda zaman serisi olarak saklanır; titreşim/sıcaklık **trend sparkline'ları** ve aşınma eğiminden **Kalan Ömür (RUL)** tahmini üretilir.
- **Anomali tespiti & alarmlar** — **eşik tabanlı** (sıcaklık/titreşim/aşınma/sağlık limitleri) + **istatistiksel** (makinenin kendi geçmişine göre z-skoru) anomali tespiti; raised→resolved yaşam döngülü alarmlar, canlı **Alarmlar** sayfası ve üst barda canlı alarm zili. *(İleride gerçek MES verisiyle eğitilecek AI arıza-tahmin modeli bu alarm altyapısına bağlanabilir.)*
- **OEE motoru** — Availability × Performance × Quality; makine bazlı ve günlük / haftalık / aylık.
- **Canlı arayüz** — SignalR ile sayfa yenilenmeden güncellenen dashboard, üretim akışı ve KPI'lar.
- **AI bakım analitiği** — son 30 günün duruşlarına bakıp en problemli makineyi ve öneriyi üretir
  (örn. *"Dokuma-03 son 30 günde 29 kez YarnBreak nedeniyle durmuştur. Önleyici bakım önerilir."*).
- **Raporlama** — günlük / haftalık / aylık rapor + **CSV** ve **Excel** export.
- **Clean Architecture** — Domain / Application / Infrastructure / API + ayrı Simulator katmanı, CQRS (MediatR), Repository soyutlaması, FluentValidation, Serilog, Swagger.
- **Çift veritabanı sağlayıcısı** — varsayılan **SQL Server**; tek ayar ile **SQLite**'a geçip sıfır kurulumla çalışır.

---

## Mimari

```
                 ┌─────────────────────────────────────────────┐
                 │                MESLite.API                  │
                 │  Controllers · ProductionHub (SignalR)      │
                 │  Serilog · Swagger · CORS                   │
                 └───────────────┬─────────────────────────────┘
                                 │ MediatR (CQRS)
        ┌────────────────────────┼─────────────────────────────┐
        ▼                        ▼                              ▼
┌──────────────┐      ┌────────────────────┐        ┌────────────────────┐
│ MESLite.Sim. │      │ MESLite.Application│        │MESLite.Infrastructure
│ Background    │─────▶│ Queries/Handlers   │◀──────▶│ EF Core DbContext   │
│ Service       │      │ OEE & AI services  │        │ Configurations/Seed │
└──────────────┘      │ DTOs · Validation  │        └─────────┬──────────┘
        │             └─────────┬──────────┘                  │
        │ IProductionNotifier   │ interfaces                  ▼
        │ (SignalR bridge)      ▼                       ┌───────────┐
        └──────────────▶ MESLite.Domain (entities)      │ SQL Server│
                                                        │ / SQLite  │
React + TypeScript + MUI + React Query + Recharts ──────└───────────┘
        (SignalR client · canlı dashboard)
```

**Bağımlılık yönü:** `API → Infrastructure → Application → Domain` (Domain hiçbir şeye bağlı değil).
Simulator ve Application yalnızca `IProductionNotifier` arayüzüne bağlıdır; SignalR detayını API katmanı sağlar (Clean Architecture).

---

## Teknolojiler

**Backend**: .NET 10 (LTS) · ASP.NET Core Web API · Entity Framework Core 10 · SQL Server / SQLite · SignalR · MediatR (CQRS) · FluentValidation · Serilog · Swagger (Swashbuckle) · ClosedXML (Excel)

**Frontend**: React 19 · TypeScript · Vite · Material UI · TanStack React Query · Recharts · @microsoft/signalr

---

## Hızlı Başlangıç

### Seçenek A — Docker (SQL Server ile, tek komut)

```bash
docker compose up --build
```

- Arayüz:  http://localhost:8088
- API / Swagger:  http://localhost:5080/swagger
- SQL Server:  `localhost:1433` (sa / `Your_strong_Pass123`)

İlk açılışta veritabanı otomatik oluşturulur (migration) ve 30 günlük örnek veri ile beslenir; simülatör canlı veri üretmeye başlar.

### Seçenek B — Lokal geliştirme

**Gereksinimler:** .NET 10 SDK, Node.js 20+. (SQL Server opsiyonel — aşağıya bakın.)

**1) Backend**

```bash
dotnet run --project backend/MESLite.API
```

API `http://localhost:5080` üzerinde açılır (Swagger: `/swagger`). İlk açılışta DB oluşturulur,
geçmiş veri seed edilir ve simülatör çalışmaya başlar.

> **Veritabanı:** Geliştirme ortamında (`appsettings.Development.json`) varsayılan **SQLite**'tır —
> sıfır kurulum, hemen çalışır. **SQL Server** ile çalıştırmak için: çalışan bir SQL Server gerekir
> (LocalDB veya `docker compose up`); `appsettings.Development.json` içindeki `Provider`'ı `SqlServer`
> yap ya da `Database__Provider=SqlServer` + `ConnectionStrings__DefaultConnection=...` ile override et.

**2) Frontend**

```bash
cd frontend
npm install
npm run dev
```

Arayüz: http://localhost:5173 — Vite, `/api` ve `/hubs` isteklerini otomatik olarak backend'e (`http://localhost:5080`) yönlendirir.
Farklı bir backend portu için: `VITE_API_TARGET=http://localhost:XXXX npm run dev`.

---

## Veritabanı Sağlayıcısı

`appsettings.json` → `Database:Provider` ile seçilir:

| Değer        | Açıklama                                                              |
|--------------|----------------------------------------------------------------------|
| `SqlServer`  | Varsayılan. `ConnectionStrings:DefaultConnection` kullanılır. EF migration uygulanır. |
| `Sqlite`     | Sıfır kurulum. `ConnectionStrings:SqliteConnection` (varsayılan `meslite.db`). Şema `EnsureCreated` ile oluşturulur. |

Ortam değişkeniyle de geçersiz kılınabilir: `Database__Provider=Sqlite`.

EF migration'lar `backend/MESLite.Infrastructure/Persistence/Migrations` altındadır. Yeni migration:

```bash
dotnet ef migrations add <Ad> --project backend/MESLite.Infrastructure --startup-project backend/MESLite.Infrastructure
```

---

## 📡 API Uç Noktaları

| Method | Endpoint                              | Açıklama |
|--------|---------------------------------------|----------|
| GET    | `/api/machines`                       | Makine listesi (durum, operatör, anlık üretim, OEE) |
| GET    | `/api/machines/{id}`                  | Makine detayı |
| GET    | `/api/production/live?take=`          | Canlı üretim akışı |
| GET    | `/api/downtimes?days=`                | Duruş analizi (sebep dağılımı, makine bazlı, son duruşlar) |
| GET    | `/api/quality?days=`                  | Kalite özeti (üretim, hata, kalite %) |
| GET    | `/api/oee/dashboard?period=`          | OEE dashboard (`Daily`/`Weekly`/`Monthly`) |
| GET    | `/api/dashboard/kpis`                 | Üst KPI kartları |
| GET    | `/api/operators/performance?days=`    | Operatör performansı |
| GET    | `/api/reports?period=`                | Rapor (JSON) |
| GET    | `/api/reports/csv?period=`            | Rapor — CSV indir |
| GET    | `/api/reports/excel?period=`          | Rapor — Excel (.xlsx) indir |
| GET    | `/api/analytics/maintenance?days=`    | AI bakım önerileri |
| GET    | `/api/analytics/rul?windowMinutes=`   | Kalan Ömür (RUL) tahmini (makine bazlı) |
| GET    | `/api/telemetry/recent?minutes=`      | Son telemetri kayıtları (trend sparkline'ları) |
| GET    | `/api/telemetry/{machineId}?minutes=` | Tek makinenin telemetri zaman serisi (detay grafikleri) |
| GET    | `/api/alarms?activeOnly=&hours=`      | Anomali/alarm listesi (aktif veya son N saat) |

**SignalR Hub:** `/hubs/production` — olaylar: `MachineStatusChanged`, `ProductionUpdated`, `DowntimeCreated`, `OeeUpdated`, `MachineTelemetry`, `AlarmRaised`, `AlarmResolved`.

---

## OEE Hesaplaması

```
Availability = ÇalışmaSüresi / PlanlananSüre        (ÇalışmaSüresi = Planlanan − Duruş)
Performance  = GerçekÜretim / İdealÜretim            (İdealÜretim = idealHız × ÇalışmaSaati)
Quality      = (Üretim − Hata) / Üretim
OEE          = Availability × Performance × Quality
```

- Saf hesaplama mantığı `OeeCalculator` içinde, EF/DB'den bağımsız ve **unit test edilebilir**.
- Pencereler **kayan (rolling)** seçilir: `Daily` = son 24s, `Weekly` = son 7g, `Monthly` = son 30g — böylece OEE her zaman dolu bir pencere üzerinden hesaplanır.
- Duruşların pencere ile kesişimi kırpılarak (clamp) kısmi/devam eden duruşlar adil sayılır.

---

## Simülatör

`MESLite.Simulator` bir `BackgroundService`'tir ve her tick'te (varsayılan 5 sn):

- Çalışan makineler için **gerçek-zamanlı oranda** üretim üretir (OEE Performance gerçekçi kalsın diye olasılıksal yuvarlama ile).
- Makine tipine göre **rastgele duruş** oluşturur (Dokuma → ağırlıklı `YarnBreak`, Boya → `MaterialWaiting`, Kesim → `Maintenance`).
- Duran/bakımdaki makineleri olasılıkla tekrar çalıştırır.
- Kalite kaydı üretir ve OEE'yi besler.
- Tüm değişiklikleri SignalR ile yayınlar.

Ayarlar `appsettings.json` → `Simulator`:

| Anahtar | Varsayılan | Açıklama |
|--------|-----------|----------|
| `Enabled` | `true` | Simülatörü aç/kapat |
| `IntervalSeconds` | `5` | Tick aralığı |
| `SpeedFactor` | `1.0` | Üretim hız çarpanı (>1 OEE Performance'ı şişirir) |
| `OeeBroadcastEveryTicks` | `6` | OEE yayını sıklığı |

Makine profilleri (kapasite ve duruş olasılıkları) senaryoyla uyumludur:

| Tip    | İdeal hız     | Duruş olasılığı | Tipik sebep |
|--------|---------------|-----------------|-------------|
| Dokuma | 500 m/saat    | %8              | İplik kopması |
| Boya   | 1000 m/saat   | %5              | Kimyasal/malzeme bekleme |
| Kesim  | 700 parça/saat| %3              | Bıçak değişimi / bakım |

---

## Makine Sağlığı & Telemetri

Her tick'te `MachineTelemetrySimulator` her makine için fiziksel parametreleri günceller ve bir **Machine Health Score (0–100)** türetir:

| Parametre | Davranış |
|-----------|----------|
| **Load** (yük %) | Operasyon bandında rastgele yürür; aşınmayı ve sıcaklığı sürükler |
| **RPM** | Tipe göre nominal aralıkta, yüke bağlı (Dokuma 600–1200, Kesim 300–800, Boya 100–300) |
| **Wear** (aşınma) | Her üretimle artar (yük oranında); bakımda sıfırlanır |
| **Vibration** | `baz + f(aşınma, yük)` — yükseldikçe bakım riski artar |
| **Temperature** | `ortam + f(yük, aşınma)` — yükseldikçe arıza ihtimali artar |
| **Efficiency / Speed** | Sağlık düştükçe verim ve hız düşer → üretim azalır |

```
HealthScore = 100 − Wear×0.7 − max(0, Vibration−4)×4 − max(0, Temperature−75)×1.2
```

**Geri besleme:** düşük sağlık → düşük hız (daha az üretim) **ve** yükselen duruş/bakım olasılığı
(`EffectiveStopProbability`, `EffectiveMaintenanceProbability`). Sağlık 0'a inerse makine bakıma alınır;
bakım tamamlanınca aşınma temizlenir ve makine yenilenir. Değerler `MachineTelemetry` SignalR
olayıyla her tick yayınlanır ve **Makine Sağlığı** sayfasında canlı gösterilir.

Eşikler: **100** yeni · **~60** dikkat · **~30** arıza riski · **0** durdu.

### Zaman serisi & Kalan Ömür (RUL)

Her tick'te telemetri `MachineTelemetrySnapshot` tablosuna yazılır (retention: 6 saat, periyodik temizlenir).
İlk açılışta ~40 dakikalık geçmiş seed edilir. **RUL tahmini** (`PredictiveMaintenanceService`) son 30 dakikanın
aşınma değerlerine **en küçük kareler (least-squares)** doğrusu uydurur:

```
aşınmaHızı (wear/saat) = regresyon eğimi
RUL (saat) = (90 − güncelAşınma) / aşınmaHızı
```

Eğim ≤ 0 ise (bakım sonrası düşen/sabit aşınma) yakın bir arıza öngörülmez. Durum:
**Kritik** (< 1 sa veya sağlık < 30) · **Uyarı** (< 4 sa) · **İyi**. Sonuç, Makine Sağlığı sayfasında
titreşim/sıcaklık sparkline'larıyla birlikte rozet olarak gösterilir.

### Anomali Tespiti & Alarmlar

`AlarmEvaluator` her tick'te iki yöntemi birleştirir:

- **Eşik (Threshold):** sabit mühendislik limitleri — sıcaklık (≥76/84°C), titreşim (≥5/6.8 mm/s),
  aşınma (≥60/82%), sağlık (≤40/25). Aşılırsa Uyarı/Kritik.
- **İstatistiksel (Statistical):** makinenin **kendi son verilerinden** (bellekte tutulan baseline)
  ortalama±σ hesaplanır; `z = (değer − ort) / σ ≥ 3` ise anomali (z ≥ 4.5 → Kritik). Sıcaklık, titreşim
  ve RPM için "bu makine için normalden sapma"yı yakalar — eşiğe takılmayan ani sıçramalar dahil.

Alarmlar **raised → resolved** yaşam döngüsüyle `Alarm` tablosuna yazılır (metrik başına tek aktif alarm,
spam önlenir), `AlarmRaised`/`AlarmResolved` ile canlı yayınlanır. Bu kural tabanlı katman, ileride
**gerçek MES verisiyle eğitilmiş bir ML arıza-tahmin modelinin** çıktısını da aynı alarm akışına
besleyebileceğin bir genişleme noktasıdır.

---

## Arayüz Sayfaları

- **Dashboard** — KPI kartları (toplam/çalışan/duran makine, bugünkü üretim, ortalama OEE), makine bazlı OEE grafiği, AI öneri özeti.
- **Makineler** — durum renkli tablo (yeşil/kırmızı/sarı), operatör, anlık üretim, sağlık skoru, OEE.
- **Makine Sağlığı** — her makine için canlı Health Score gauge'u + RPM / hız / yük / aşınma / titreşim / sıcaklık + RUL rozeti ve trend sparkline'ları (SignalR ile gerçek zamanlı). Kartlar tıklanabilir.
- **Makine Detayı** (`/machines/:id`) — tam ekran zaman serisi grafikleri: Sağlık Skoru, Aşınma, Titreşim, Sıcaklık (canlı uzayan zaman ekseni) + anlık parametre şeridi ve RUL.
- **Alarmlar** — eşik + istatistiksel anomali alarmları, aktif/çözüldü durumu, canlı (SignalR); üst barda aktif alarm sayısını gösteren zil.
- **Üretim** — SignalR ile gerçek zamanlı üretim akışı.
- **Duruşlar** — sebep dağılımı (pasta), makine bazlı toplam (bar), son duruşlar.
- **Kalite** — toplam üretim/hata/kalite %, makine bazlı hata grafiği.
- **Operatörler** — operatör performans tablosu.
- **Raporlar** — dönem seçimi + CSV / Excel export.

---

## Testler

```bash
dotnet test
```

`MESLite.Tests`:
- `OeeCalculatorTests` — OEE matematiğinin saf birim testleri (availability/performance/quality/clamp).
- `OeeCalculationServiceTests` — in-memory SQLite üzerinde uçtan uca OEE hesaplama (EF sorguları + duruş kesişimi).

---

## Proje Yapısı

```
mes-lite/
├─ MESLite.slnx
├─ docker-compose.yml
├─ backend/
│  ├─ MESLite.Domain/          # Entity'ler, enum'lar (bağımsız)
│  ├─ MESLite.Application/     # CQRS (MediatR), DTO, OEE & AI servisleri, validation
│  ├─ MESLite.Infrastructure/  # EF Core DbContext, configuration, seed, migrations
│  ├─ MESLite.Simulator/       # BackgroundService üretim simülatörü
│  ├─ MESLite.API/             # Controllers, SignalR Hub, Program.cs, Dockerfile
│  └─ MESLite.Tests/           # xUnit testleri
└─ frontend/                   # React + TS + MUI + React Query + Recharts + SignalR
```

---

## Lisans

Portföy / eğitim amaçlı örnek proje.
