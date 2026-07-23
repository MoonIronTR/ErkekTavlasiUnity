Bu projede gorseller dis dosya olarak degil, C# kodu ile ciziliyor.

Neden?
- C#'a yeni baslayan biri icin MeshGeometry3D, Material, Light, Camera gibi 3D yapilari gormek daha ogretici.
- Tavla tahtasi, taslar, zarlar ve animasyonlar kod icinde kolayca degistirilebilir.
- Ileride Monte Carlo AI eklenirken oyun kurallari gorselden ayri kaldigi icin simulasyon yapmak kolay olur.

Gorsel kodlari:
- Ui/MainWindow.cs: 3D tahta, tas, zar, secim ve animasyon cizimleri.
- Game/GameState.cs: kurallar ve hamle uretimi.
