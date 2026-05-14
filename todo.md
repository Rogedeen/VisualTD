# Yapılacaklar Listesi (To-Do List)

## Faz 1 - Python Görü & Mimari Planlama
- [ ] Python ortamını başlat ve kütüphaneleri kur (OpenCV, MediaPipe).
- [ ] MediaPipe kullanarak temel el takibini gerçekleştir.
- [ ] "Ok Yağmuru" (Arrow Volley) hareketini tanıyacak algoritmayı geliştir.
- [ ] "Yıldırım Düşmesi" (Lightning Strike) hareketini tanıyacak algoritmayı geliştir.
- [ ] "Duvarı İyileştir" (Fortify/Heal Wall) hareketini tanıyacak algoritmayı geliştir.
- [ ] Algılanan hareketleri konsola yazdır.

## Faz 2 - Köprü (The Bridge)
- [x] JSON verilerini serileştirip göndermek için Python'da UDP Sender oluştur.
- [x] Unity C#'ta asenkron UDP Receiver (`UDPReceiver.cs`) oluştur.
- [x] Unity tarafında ana iş parçacığını dondurmadan JSON verisi ayrıştırma (parsing) işlemini gerçekleştir.
- [x] Python ve Unity arasındaki uçtan uca iletişimi test et.

## Faz 3 - Temel Oyun Sistemleri
- [ ] Objelerin verimli yönetimi için `ObjectPooler.cs` oluştur.
- [ ] NavMesh entegrasyonu ile `EnemyManager.cs` ve `EnemyAI.cs` oluştur.
- [ ] `SkillManager.cs` oluştur ve yetenek yapılarını tanımla.
- [ ] Kapı/Sur can yönetimi için `CastleManager.cs` oluştur.
- [ ] Unity Inspector değişkenlerini ayarla ve kullanıcı için NavMesh talimatlarını hazırla.

## Faz 4 - Entegrasyon & Cila
- [ ] UDP'den gelen verilerle asıl yetenekleri tetiklemek için `GestureParser.cs` ile `SkillManager.cs` bağlantısını kur.
- [ ] KayKit animasyonlarını (Idle, Walk, Attack, Die) bağla.
- [ ] Yetenekler için görsel parçacık efektlerini (düşen oklar, yıldırım, iyileştirme aurası) ekle.
- [ ] Oynanış mekaniklerini test et ve dengele (balance).
