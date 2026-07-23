# Erkek Tavlasi - C# Ogrenme Projesi

Bu proje basit bir WPF 3D tavla oyunudur. Dis oyun motoru kullanmaz; tahta, taslar ve zarlar C# `Viewport3D` ile 3 boyutlu sahnede cizilir.

## Calistirma

```powershell
cd C:\Users\<kullanici_adin>\Desktop\VIBECODE\ErkekTavlasiUnity\LegacyPrototypes\WpfPrototype
dotnet run
```

## Oynanis

- `Zar At` butonu ile sira oyuncusu zar atar.
- Once oynatmak istedigin tasi sec.
- Sonra hafif yesil isaretlenen hedef haneye tikla.
- Yesil hedefler sadece tek zari degil, sirali zar kombinasyonlarini da gosterir. Ornek: 5 ve 3 atinca uygun durumlarda 5, 3 ve 8 uzakliklari; cift 4 atinca 4, 8, 12 ve 16 uzakliklari gorunur.
- Kirik tas varsa once bar alanindan oyuna sokman gerekir.
- Zarlar 3D tahtanin ustunde sekerek ve donerek atilir.
- Hamle kalmadiginda ya da zarlar bittiginde sira otomatik degisir.
- Tum taslar kendi ev bolgesindeyken tas toplama baslar.

## Monte Carlo AI icin hazirlik

AI ileride `Game/IPlayer.cs` arayuzunu kullanabilir.
Monte Carlo simulasyonu icin en onemli parcalar hazir:

- `GameState.Clone()`
- `GameState.GetLegalMoves()`
- `GameState.ApplyMove(...)`
- `GameState.RollDice(...)`
- `GameState.IsGameOver()`

Bu sayede AI, gercek tahtayi bozmadan kopya oyunlar uzerinde rastgele denemeler yapabilir.
