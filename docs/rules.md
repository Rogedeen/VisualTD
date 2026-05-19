# VisualTD Geliştirme Kuralları (Project Rules)

Bu belge, projedeki kod kalitesini artırmak, performans sorunlarını önlemek ve Unity'nin modern standartlarına uyum sağlamak için oluşturulmuştur. Sonraki geliştirmelerde bu kurallara uyulması KRİTİKTİR.

## 1. Unity API Standartları (2023+ Uyumluluk)

*   **Find Metotları:** `FindObjectOfType` ve `FindObjectsOfType` kullanımından kaçının (Obsolete).
    *   Tek obje ararken: `Object.FindAnyObjectByType<T>()` kullanın (Daha performanslıdır).
    *   Liste ararken: `Object.FindObjectsByType<T>(FindObjectsInactive.Exclude)` kullanın.
    *   Sıralama gerekmiyorsa `FindObjectsSortMode` parametresini ASLA göndermeyin.
*   **Warp vs Position:** NavMeshAgent kullanan objeleri ışınlarken her zaman `agent.Warp(position)` kullanın. Direct transform ataması NavMesh hesaplamalarını bozabilir.
*   **Carving:** Dinamik engel oluşturan yapılar (Kapı, Sur vb.) için `NavMeshObstacle` kullanın ve `Carve` seçeneğini işaretleyin.

## 2. Mimari Kararlar ve Singleton Kullanımı

*   **Manager Yapıları:** `GameManager`, `UIManager`, `TargetManager`, `SkillManager` ve `CoreManager` birer **Singleton**'dır.
    *   Erişim: `XManager.Instance` üzerinden yapılmalıdır.
    *   Instance kontrolü `Awake` içerisinde yapılmalı ve `DontDestroyOnLoad` ihtiyaca göre değerlendirilmelidir.
*   **Düşman Verileri:** Yeni bir iskelet tipi eklerken kod yazmayın. `EnemyData` ScriptableObject'ini kullanın.
*   **Ekonomi:** Altın ödülleri `EnemyData` üzerinden tanımlanır, `EnemyAI` öldüğünde `GameManager.Instance.AddGold()` çağrılır.

## 3. Performans ve Optimizasyon

*   **Update Yerine Coroutine:** Sürekli polleme yapan (Örn: Mesafe kontrolü, hedef arama) işlemleri `Update` yerine `IEnumerator` veya timer tabanlı (`Time.time > lastCheck + interval`) yapılarla yönetin.
*   **Component Caching:** `GetComponent` çağrılarını `Update` içerisinde yapmayın. `Awake` veya `Start` içinde cache'leyin.
*   **Y-Axis Lock:** Root Motion kullanılan animasyonlarda karakterin yerden yükselmesini önlemek için `Update` içinde `transform.position = new Vector3(x, fixedY, z)` kilitlemesi uygulayın.

## 4. UI ve Geri Bildirim

*   **UIToolkit:** Projede UI için UElements (UI Toolkit) kullanılmaktadır. Label güncellemek için `root.Q<Label>("Name")` ile query yapın.
*   **HealthBar:** Her saldırı alan nesne `HealthBar` ve `DamageFlash` bileşenlerine sahip olmalıdır.

## 5. İletişim ve Gesture Entegrasyonu

*   **UDP & Gesture:** El hareketleri `UDPReceiver` üzerinden gelir ve `GestureParser` tarafından işlenir. Büyü tetiklemeleri için `SkillManager` metodlarını çağırın.
*   **Cooldown:** Tüm becerilerin `SkillManager` içinde tanımlanmış bir `cooldown` süresi olmalı ve bu süre dolmadan beceri tetiklenmemelidir.

## 6. Animasyon ve Rig Standartları (Rigging & Animations)

*   **Shared Rig:** Tüm karakterler (Dünyadaki dost/düşman herkes) `Humanoid` rig tipinde olmalı ve `Assets/Animations/UniversalSkeletonAvatar.asset` avatarını kullanmalıdır.
*   **Animation Looping:** KayKit veya benzeri harici assetlerin `Loop Time` ayarları Model Importer üzerinden her zaman aktif (Checked) olmalıdır.
*   **Animator Sync:** Yeni bir "İnsan" karakter (Commander, Mage vb.) eklendiğinde, `isHolding` ve `isFortifying` gibi global gesture parametreleri Animator Controller'da tanımlı olmalı ve `SkillManager` tarafından yönetilmelidir.
*   **Agent Sync:** `NavMeshAgent` kullanılan karakterlerde titremeyi (jitter) önlemek için `agent.updatePosition = false` yapılmalı ve `Update` içerisinde `transform.position = agent.nextPosition` ile manuel senkronizasyon sağlanmalıdır.
