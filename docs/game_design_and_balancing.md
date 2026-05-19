# VisualTD: Game Design & Balancing Master Document

Bu doküman, oyunun yeni mimarisini (Bina türlerinin ayrılması), oyun döngüsünü (Game Over, Core HP) ve matematiksel dengeleme (Balancing) verilerini içerir. Yeni eklenecek kodlar ve ayarlar bu belgedeki standartlara göre yapılmalıdır.

---

## 1. MİMARİ VE OYUN DÖNGÜSÜ (ARCHITECTURE & GAME LOOP)

Eski sistemdeki tek parça `CastleManager` yapısı parçalanacak ve aşağıdaki sisteme geçilecektir:

### A. Bina Türleri ve Davranışları
1. **Gate (Ana Kapı):** Düşmanların birincil hedefi. En yüksek cana sahip yapı.
2. **Wall (Duvarlar):** Kapının etrafını saran yapılar. Düşmanlar NavMesh üzerinde sadece Gate'e giden yol tamamen tıkanmışsa (veya kapıdan daha mantıklı/kısa bir yol sunuyorsa) duvarlara saldırır.
3. **Tower (Kuleler):** Üzerinde `ArcherAI` barındıran yapılar.
   * **Özel Mekanik:** Kule yıkıldığında (HP = 0), bir event (`OnTowerDestroyed`) tetiklenir. Üzerindeki Okçu objesinin `Rigidbody`'si aktif edilir, okçu yerçekimiyle aşağı düşer, `ArcherAI` scripti kapanır ve ölüm animasyonu oynar.
4. **Core (Kale İçi / Oyuncu Canı):** Kapı veya Duvar yıkıldığında, NavMesh üzerindeki Engel (Obstacle) kalkar. Düşmanlar kalenin içine akın eder. İçeride görünmez bir `BoxCollider` (Trigger) olacak. Bu çizgi geçildiğinde Düşman kendini yok edecek ve **Player Health (Core HP)** azalacak.

### B. Oyun Döngüsü (Game Loop)
* **Player Health:** Oyuncunun canı (Örn: 20 Can). Her düşman içeri sızdığında 1 azalır.
* **Game Over:** Player Health 0 olduğunda oyun durur (Time.timeScale = 0), Ekrana "Defeat" menüsü gelir ve Restart butonu çıkar.
* **Wave Manager:** Düşmanlar `EnemyManager` tarafından belirli aralıklarla (Dalgalar halinde) ObjectPooler kullanılarak spawn edilir.

---

## 2. MATEMATİKSEL DENGELEME (OBJECTIVE BALANCING)

Bu sayılar, 3 okçuluk bir kule savunması senaryosu ve Kingdom Rush tarzı oyunlar baz alınarak "Base Tier 1" (Başlangıç Seviyesi) için hesaplanmıştır.

### A. Savunma Birimleri (Player DPS)
Okçuların hasarı, düşman dalgalarını duvarlara çok fazla zarar vermeden eritebilecek seviyede olmalıdır.

| Birim | Hasar (Damage) | Atış Hızı (Fire Rate) | Menzil (Range) | DPS (Saniye Başına Hasar) |
| :--- | :--- | :--- | :--- | :--- |
| **Okçu (Archer)** | 15 - 20 | 1.5 Saniye | 15 Birim | ~12 |
| *Matematiksel Not:* 3 Okçu saniyede toplam ~36 hasar çıkarır. Yani 50 canlı bir düşmanı yaklaşık 1.5 saniyede öldürebilirler.

### B. Düşman Birimleri (Enemy Stats)
Düşmanların hareket hızı, okçuların menzili içinde ne kadar süre kalacaklarını belirler. Menzil 15, Hız 2.5 ise bir düşman hedefe ulaşmadan önce okçuların menzilinde yaklaşık 6 saniye kalır.

| Birim | Can (HP) | Hareket Hızı (Speed) | Vuruş Hasarı | Atak Hızı (Cooldown) | DPS |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **İskelet (Skeleton)** | 50 | 2.5 Birim | 5 | 1.5 Saniye | ~3.3 |
| *Matematiksel Not:* Bir iskelet duvara ulaşırsa saniyede ~3.3 hasar verir. 10 iskelet duvara ulaştığında saniyede 33 hasar verirler.

### C. Bina Canları (Structure Health)
Binaların canı, oyuncuya "Skill (Yetenek)" kullanması için yeterli zamanı tanımalıdır.

| Bina Türü | Can (HP) | Dayanma Süresi (10 İskelet Saldırırsa) |
| :--- | :--- | :--- |
| **Gate (Kapı)** | 500 | ~15 Saniye (Müdahale için ideal süre) |
| **Wall (Duvar)** | 300 | ~9 Saniye |
| **Tower (Kule)** | 150 | ~4.5 Saniye (Kuleler çok kırılgandır, korunmalıdır) |
| **Player Core** | 20 (Adet) | İçeri 20 iskelet girerse Game Over. |

### D. Büyüler ve Skiller (Skills Integration)
Yetenekler, okçuların yetersiz kaldığı anlarda (duvarda yığılma olduğunda) kurtarıcı olmalıdır. Bekleme süreleri (Cooldown) sürekli spamlalamayı önler.

| Yetenek (Gesture) | Etki (Effect) | Sayısal Değer | Cooldown | Amaç |
| :--- | :--- | :--- | :--- | :--- |
| **Arrow Volley** (El Açma) | Alan Hasarı (AoE) | 10 Ok x 20 Hasar = Toplam 200 Hasar (Alana Dağıtılır) | 15 Saniye | Kalabalık "Zayıf" düşman yığınlarını temizlemek. |
| **Lightning Strike** (Aşağı Çekme)| Yüksek Tekli Hasar | Küçük Alanda 150 Hasar (Çarpılma Etkisi) | 30 Saniye | Canı yüksek olan (Örn: Boss veya Zırhlı) düşmanları tek seferde eritmek. |
| **Fortify / Heal** (Dur İşareti) | Bina Canı Yenileme | Hedef Binaya +150 HP | 20 Saniye | Yıkılmak üzere olan kapıyı/duvarı kritik anda kurtarmak. |

---
**ALTIN KURAL:** Yeni ajan, `EnemyAI.cs`, `ArcherAI.cs` ve oluşturulacak yeni Bina scriptlerinde (Örn: `StructureManager.cs`) yukarıdaki değerleri `[SerializeField]` olarak Unity Inspector'a açmalı ve bu varsayılan (default) değerleri atamalıdır. Herhangi bir magic number (sabit sayı) kullanmak yasaktır!
