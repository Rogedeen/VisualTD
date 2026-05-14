# Mimari Plan (Architecture Blueprint)

## Sistem Özeti
Sistem iki ana bileşenden oluşmaktadır:
1. Python Bilgisayarlı Görü Modülü (MediaPipe)
2. Unity 3D Kule Savunma Oyunu (C#)
Bu iki sistem arasındaki iletişim Asenkron UDP Soketleri üzerinden sağlanır.

## Python Bilgisayarlı Görü Modülü
- **El Takibi**: 21 el eklemini tespit etmek için MediaPipe Hands.
- **Hareket (Gesture) Tanıma**: Eklem verilerini analiz ederek üç dinamik hareketi sınıflandırır:
  1. Ok Yağmuru (Arrow Volley): Kapalı yumruk -> Elin ileri doğru itilip açılması.
  2. Yıldırım Düşmesi (Lightning Strike): İşaret parmağı yukarı bakıyor -> Hızlıca aşağıya indirme.
  3. Duvarı İyileştir/Güçlendir (Fortify/Heal Wall): İki açık el kameraya bakıyor (Dur hareketi).
- **UDP Gönderici (Sender)**: Tanınan hareketleri JSON formatına dönüştürüp UDP üzerinden Unity arka planına gönderir.

## Unity Oyun Modülü
- **UDPReceiver.cs**: Gelen UDP paketlerini asenkron olarak dinler, JSON verisini ayrıştırır ve ana iş parçacığı (main thread) için işlemleri sıraya koyar.
- **GestureParser.cs**: Sıradaki işlemleri okur ve oyun içi yeteneklerle eşleştirir.
- **SkillManager.cs**: Eşleştirilen yetenekleri çalıştırır (Ok Yağmuru, Yıldırım Düşmesi, Duvar İyileştirme).
- **EnemyManager.cs & EnemyAI.cs**: Düşmanları yaratır, kapıya doğru NavMesh rotalarını ayarlar ve düşmanların can/durum yönetimini yapar.
- **ObjectPooler.cs**: Çöp toplayıcı (garbage collection) takılmalarını önlemek ve performansı korumak için oklar, düşmanlar ve parçacık efektlerinin (particle effects) havuzlama yönetimini sağlar.
- **CastleManager.cs**: Ana kapının/surların canını ve durumunu yönetir.

## İletişim Protokolü
- Protokol: Düşük gecikme (latency) için UDP.
- Format: JSON
- Örnek Veri: `{"gesture": "arrow_volley", "confidence": 0.95}`
