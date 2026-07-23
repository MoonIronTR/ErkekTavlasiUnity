# C# Ogrenme Notlari

Bu proje bilerek sade tutuldu. Okurken su sirayla gitmen iyi olur:

1. `Program.cs`
   - Programin basladigi yer.
   - WPF uygulamasini acar ve `MainWindow` penceresini calistirir.

2. `Game/PlayerColor.cs`
   - `enum` ornegi.
   - Oyuncu rengini `White`, `Black`, `None` diye tutar.

3. `Game/PointStack.cs`
   - Basit `class` ornegi.
   - Bir hanede kimin kac tasi oldugunu saklar.

4. `Game/BackgammonMove.cs`
   - Bir hamlenin nereden nereye gittigini saklar.
   - `FromBar` ve `ToOffBoard` sabitleri ozel durumlar icindir.

5. `Game/GameState.cs`
   - Oyunun beynidir.
   - Zar atma, yasal hamle bulma, hamle uygulama, tas kirma ve tas toplama burada.
   - Monte Carlo AI en cok bu dosyayi kullanacak.

6. `Ui/MainWindow.cs`
   - Oyunun ekrani.
   - 3 boyutlu tahta, tas, zar ve animasyonlar burada cizilir.
   - `Viewport3D` sahneyi gosterir.
   - `MouseDown` tiklamalari yakalar.
   - `DispatcherTimer` zar ve tas animasyonlarini ilerletir.

## Monte Carlo AI icin sonraki adim

Bir Monte Carlo oyuncusu su mantikla yazilabilir:

1. Mevcut durumun kopyasini al: `GameState.Clone()`.
2. Yasal hamleleri bul: `GetLegalMoves()`.
3. Her hamle icin cok sayida rastgele oyun oynat.
4. En cok kazandiran hamleyi sec.

Bu yuzden oyun kurallari `Ui` klasorunde degil, `Game` klasorunde tutuldu.
