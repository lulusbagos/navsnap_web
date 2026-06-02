# Scope Modul NavSnap

Dokumen ini menjelaskan modul tambahan sesuai request user dan implementasi teknisnya di aplikasi NavSnap Web.

## Modul dan URL

| Modul | URL | Hak Akses Default | Tabel |
| --- | --- | --- | --- |
| Sales Tracking | `/SalesTracking/Index` | Administrator, Pengawas, Sales | `tbl_t_gps_logs`, `tbl_t_sales_visits` |
| Laporan Kunjungan | `/SalesVisitReport/Index` | Administrator, Pengawas, Sales | `tbl_t_sales_visit_reports` |
| Kirim Payroll Otomatis | `/PayrollAutoSend/Index` | Administrator, Pengawas | `tbl_t_payroll_auto_sends` |
| Pengajuan WFH/WFA | `/WorkArrangement/Index` | Administrator, Pengawas, Sales | `tbl_t_work_arrangement_requests` |
| Compliance Target | `/SalesCompliance/Index` | Administrator, Pengawas | `tbl_t_sales_target_compliances` |
| Rekrutmen | `/Recruitment/Index` | Administrator, Pengawas | `tbl_t_recruitment_candidates` |
| Onboarding | `/Onboarding/Index` | Administrator, Pengawas | `tbl_t_onboarding_tasks` |
| Ringkasan CV | `/CvSummary/Index` | Administrator, Pengawas | `tbl_t_cv_master_summaries` |

## Ringkasan Fitur

### Laporan Kunjungan

Fungsi:
- CRUD laporan kunjungan sales.
- Relasi ke user sales dan checkpoint.
- Menyimpan tanggal report, status kunjungan, outcome, dan catatan.

Data dummy:
- Dibuat dari kombinasi sales dan checkpoint aktif.

Flow proses:
1. Sales/Admin menambahkan laporan kunjungan setelah aktivitas lapangan.
2. Pengawas memfilter laporan berdasarkan sales, status, dan periode.
3. Pengawas/Admin mengekspor CSV untuk rekap harian, mingguan, atau kebutuhan audit.

### Kirim Payroll Otomatis

Fungsi:
- CRUD jadwal pengiriman payroll/slip gaji.
- Menyimpan periode payroll, tanggal kirim, status, jumlah penerima, catatan, dan waktu terkirim.
- Aksi cepat `Mark Sent` untuk menandai payroll sudah terkirim.

Data dummy:
- Periode payroll Mei 2026 dengan status `sent`.
- Periode payroll Juni 2026 dengan status `scheduled`.

Flow proses:
1. Admin/Pengawas membuat jadwal pengiriman payroll per periode.
2. Status awal adalah `scheduled`.
3. Tombol `Mark Sent` menandai payroll terkirim dan mengisi waktu kirim.
4. Status `failed` atau `cancelled` dipakai untuk exception dan tidak menyimpan waktu terkirim.

### Pengajuan WFH/WFA

Fungsi:
- CRUD pengajuan kerja WFH/WFA.
- User Sales dapat membuat pengajuan.
- Administrator/Pengawas dapat approve/reject.
- Menyimpan user, periode kerja, tipe WFH/WFA, lokasi, alasan, status, approver, dan waktu approval.

Data dummy:
- Pengajuan WFH pending.
- Pengajuan WFA approved.

Flow proses:
1. User membuat pengajuan WFH/WFA dengan periode, lokasi, dan alasan.
2. Admin/Pengawas melakukan approve atau reject.
3. Status final menyimpan approver dan waktu approval.
4. User non-approver hanya melihat dan mengelola data miliknya sendiri.

### Compliance Target

Fungsi:
- CRUD monitoring target vs realisasi kunjungan sales.
- Compliance percent dihitung otomatis dari `ActualVisits / TargetVisits`.
- Status otomatis:
  - `compliant`: minimal 90%.
  - `watch`: 75% sampai di bawah 90%.
  - `risk`: di bawah 75%.

Data dummy:
- Dibuat dari beberapa user sales aktif.

Flow proses:
1. Admin/Pengawas menginput target dan realisasi kunjungan.
2. Sistem menghitung compliance percent otomatis.
3. Status otomatis menjadi `compliant`, `watch`, atau `risk`.
4. Report bisa difilter dan diekspor CSV untuk review KPI.

### Rekrutmen

Fungsi:
- CRUD kandidat recruitment.
- Menyimpan nama, email, telepon, posisi, stage, status, source, dan tanggal dibuat.

Data dummy:
- Kandidat untuk Sales Executive, Area Sales, dan HR Officer.

Flow proses:
1. HR/Admin menambahkan kandidat baru.
2. Stage kandidat dipindahkan dari screening, interview, offering, sampai hired.
3. Status kandidat membantu monitoring pipeline aktif, hired, rejected, atau hold.

### Onboarding

Fungsi:
- CRUD task onboarding.
- Bisa terhubung ke kandidat recruitment.
- Menyimpan nama employee, task, due date, status, owner, dan catatan.

Data dummy:
- Task kelengkapan dokumen untuk kandidat dummy.

Flow proses:
1. HR/Admin membuat checklist onboarding untuk kandidat atau karyawan baru.
2. Owner task memantau status `open`, `in_progress`, `done`, atau `blocked`.
3. Task overdue terlihat dari due date pada daftar onboarding.

### Ringkasan CV

Fungsi:
- CRUD ringkasan CV kandidat.
- Bisa terhubung ke kandidat recruitment.
- Menyimpan nama kandidat, posisi, pendidikan terakhir, pengalaman, skills, summary, dan score.

Data dummy:
- Summary CV untuk kandidat dummy.

Flow proses:
1. HR/Admin membuat ringkasan CV dari kandidat recruitment atau input manual.
2. Score membantu prioritas review kandidat.
3. Score hijau untuk kandidat kuat, kuning untuk perlu review, merah untuk kandidat berisiko rendah.

## Menu Sidebar

Struktur menu yang ditambahkan:

- Sales Tracking
  - Live Tracking
  - Riwayat Kunjungan
- Sales Report
  - Laporan Kunjungan
  - Compliance Target
- Sales Planning
  - Target Harian
  - Jadwal Kunjungan
- Payroll & Attendance
  - Setting Lokasi Absen
  - Laporan Absen
  - Izin & Lembur
  - Kirim Payroll Otomatis
  - Pengajuan WFH/WFA
- Talent Management
  - Rekrutmen
  - Onboarding
  - Ringkasan CV

## Seeder dan Schema

Schema version dinaikkan ke `v13`.

Seeder membuat tabel baru dengan `CREATE TABLE IF NOT EXISTS`, sehingga aman dijalankan berulang. Menu, role access, dan dummy data juga dicek terlebih dahulu agar tidak terjadi duplikasi.

Untuk menerapkan schema, menu, dan dummy data baru, pastikan `DbSeeder.SeedAsync` dijalankan pada startup atau melalui konfigurasi seeder yang tersedia di environment. Pada setup yang memakai toggle seeder, gunakan:

```powershell
$env:NAVSNAP_RUN_SEEDER="1"
dotnet run --project .\NavSnap\NavSnap.csproj
```

atau set konfigurasi:

```json
{
  "RunSeeder": true
}
```

## Catatan Teknis

- Semua modul menggunakan pola ASP.NET Core MVC dan EF Core yang sudah ada.
- Routing mengikuti default `{controller}/{action}/{id?}`.
- Semua form memakai anti-forgery token.
- Delete menggunakan POST.
- Relasi utama:
  - report kunjungan ke `User` dan `Checkpoint`.
  - WFH/WFA ke `User` dan approver.
  - compliance ke `User`.
  - onboarding dan CV summary ke kandidat recruitment.
