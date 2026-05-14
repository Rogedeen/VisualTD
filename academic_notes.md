# Akademik Notlar

## Neden TCP yerine UDP?
Gerçek zamanlı el hareketi kontrollü bir oyunda gecikme (latency) kritik bir faktördür. TCP (Transmission Control Protocol) bağlantı odaklıdır ve veri iletimini garanti altına alır; ancak bu, ek yük oluşturur ve paketler kaybolduğunda tekrar iletim nedeniyle gecikmelere (Head-of-line blocking) sebep olabilir. UDP (User Datagram Protocol) ise bağlantısızdır ve paketin ulaşmasını garanti etmeden gönderir, bu da çok daha düşük bir gecikme sağlar. Bizim durumumuzda, tek bir karenin (frame) hareket verisinin kaybolması kabul edilebilirdir ve diğer karelerin gecikmesinden çok daha iyidir. Bu nedenle UDP bu proje için en ideal seçimdir.

## MediaPipe El Takibi (21 Eklem)
MediaPipe Hands, tek bir kareden bir elin 21 adet 3D eklem noktasını (landmark) tahmin etmek için makine öğrenimi modellerinden yararlanır. Model, eklemlerin kesin konumlarını sağlayarak eklemler arasındaki mesafeleri ve açıları hesaplamamıza olanak tanır. Örneğin, bir elin kapalı (yumruk) olup olmadığını belirlemek için parmak uçları ile avuç içi arasındaki mesafeye bakılır. Bu eklem konumlarının zamansal sırasını (hareketini) analiz ederek, "hızlıca aşağıya indirme" veya "eli ileri doğru itme" gibi dinamik hareketleri tanımlayabilir ve tanıyabiliriz.

## Unity'de SOLID Prensipleri
SOLID prensiplerinin uygulanması, kodun ölçeklenebilir ve bakımı kolay olmasını sağlar:
- **Tek Sorumluluk (Single Responsibility)**: `UDPReceiver` sadece ağ trafiğini yönetirken, `SkillManager` sadece yeteneklerin çalıştırılmasını yönetir.
- **Açık/Kapalı (Open/Closed)**: Ortak bir `ISkill` arayüzü ile uygulandığı takdirde, yeni yeteneklerin eklenmesi ana `SkillManager` sınıfında değişiklik yapılmasını gerektirmez.
- **Bağımlılıkları Tersine Çevirme (Dependency Inversion)**: Yüksek seviyeli modüller (ör. `SkillManager`), düşük seviyeli modüllere (ör. `UDPReceiver`) doğrudan bağımlı olmaz; bunun yerine soyutlamalara veya olay tabanlı (event-based) iletişime güvenir.

## Performans İçin Nesne Havuzlama (Object Pooling)
Unity'de, sürekli olarak GameObject'leri (oklar veya düşmanlar gibi) Instantiate (yaratma) ve Destroy (yok etme) işlemleri, çöp toplayıcıyı (garbage collection) tetikler ve bellek tahsisine yol açar, bu da kare hızı düşüşlerine (FPS droplara/takılmalara) neden olur. Nesne Havuzlama (Object Pooling), belirli sayıda nesneyi önceden yaratarak ve onları devre dışı bırakarak (disable) bu sorunu çözer. Bir nesneye ihtiyaç duyulduğunda, etkinleştirilir (enable) ve yeniden konumlandırılır. "Yok edildiğinde" ise basitçe devre dışı bırakılır ve havuza geri döner, böylece CPU üzerindeki yük büyük ölçüde azalır.
