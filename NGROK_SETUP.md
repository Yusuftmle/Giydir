# Ngrok Kurulumu - Replicate API için Public URL

## Sorun
Replicate API dışarıdan localhost'a erişemez. Görsellerin public bir URL'den servis edilmesi gerekiyor.

## Çözüm: Ngrok

### 1. Ngrok Kurulumu

**Windows için:**
1. https://ngrok.com/download adresinden indir
2. ZIP'i aç ve `ngrok.exe`'yi PATH'e ekle veya proje klasörüne kopyala

**Alternatif (Chocolatey ile):**
```powershell
choco install ngrok
```

### 2. Ngrok Hesabı Oluştur
1. https://dashboard.ngrok.com/signup adresinden ücretsiz hesap oluştur
2. Auth token'ı al: https://dashboard.ngrok.com/get-started/your-authtoken

### 3. Ngrok'u Başlat

Terminal'de (uygulamanın çalıştığı port):
```bash
ngrok http 5267
```

Veya auth token ile:
```bash
ngrok config add-authtoken YOUR_AUTH_TOKEN
ngrok http 5267
```

### 4. Ngrok URL'sini Kullan

Ngrok başladığında şöyle bir URL alırsın:
```
Forwarding: https://xxxx-xxxx-xxxx.ngrok-free.app -> http://localhost:5267
```

Bu URL'yi `appsettings.json`'da kullan:
```json
{
  "BaseUrl": "https://xxxx-xxxx-xxxx.ngrok-free.app"
}
```

### 5. Uygulamayı Yeniden Başlat

BaseUrl'i güncelledikten sonra uygulamayı yeniden başlat.

## Alternatif: Cloudflare Tunnel (Ücretsiz, Kalıcı)

```bash
# Cloudflare Tunnel kurulumu
cloudflared tunnel --url http://localhost:5267
```

## Notlar

- Ngrok free plan'da URL her restart'ta değişir
- Production için gerçek domain kullan
- Görselleri Cloudinary/Imgur gibi servislere yüklemek de bir alternatif



