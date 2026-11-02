Shader "RhythmGame/AnimatedNeonBackground"
{
    Properties
    {
        _MainTex ("Background (hien tai)", 2D) = "black" {}
        _PrevTex ("Background (truoc do)", 2D) = "black" {}
        _SpecTex ("Spectrum 16x1", 2D) = "black" {}
        _Blend ("Crossfade", Range(0,1)) = 1
        _UVScale ("UV Scale", Vector) = (1,1,0,0)
        _UVOffset ("UV Offset", Vector) = (0,0,0,0)
        _Zoom ("Zoom", Float) = 1.08
        _Aspect ("Image Aspect", Float) = 1.7778
        _ScreenAspect ("Screen Aspect", Float) = 1.7778
        _Pulse ("Pulse", Float) = 0
        _Energy ("Energy", Float) = 0
        _HueShift ("Hue Shift (radian)", Float) = 0
        _NeonBoost ("Neon Boost", Float) = 1.2
        _FloorY ("Floor Top (image uv)", Float) = 0.34
        _FloorWave ("Floor Wave", Float) = 0.004
        _BeamStrength ("Beam Strength", Float) = 0.35
        _BeamO0 ("Beam Origin 0", Vector) = (0.44,0.9,0,0)
        _BeamO1 ("Beam Origin 1", Vector) = (0.51,0.9,0,0)
        _BeamO2 ("Beam Origin 2", Vector) = (0.556,0.9,0,0)
        _BeamO3 ("Beam Origin 3", Vector) = (0.604,0.9,0,0)
        _BeamColA ("Beam Color A", Color) = (0.2,0.85,1,1)
        _BeamColB ("Beam Color B", Color) = (1,0.25,0.85,1)
        _Flicker ("Flicker", Float) = 0.15
        _LaneCenter ("Lane Center (screen x)", Float) = 0.5
        _LaneHalf ("Lane Half Width", Float) = 0.17
        _LaneSoft ("Lane Softness", Float) = 0.12
        _LaneDim ("Lane Dim", Float) = 0.35
        _Vignette ("Vignette", Float) = 0.35

        _BarStrength ("Equalizer Strength", Float) = 0.5
        _BarHeight ("Equalizer Height", Float) = 0.35
        _BarInner ("Equalizer Inner Edge", Float) = 0.30
        _ParticleStrength ("Particle Strength", Float) = 0.6
        _Aberration ("Chromatic Aberration", Float) = 0.004
        _Focus ("Focus (x, y, radius)", Vector) = (0.58,0.74,0.12,0)
        _FocusCol ("Focus Color", Color) = (1,0.7,0.85,1)
        _FocusStrength ("Focus Strength", Float) = 0.6
        _Ripple ("Ripple (progress, amount)", Vector) = (1,0,0,0)
        _RippleCenter ("Ripple Center (screen)", Vector) = (0.5,0.35,0,0)
        _Twinkle ("Window Twinkle", Float) = 0.5
        _TwinkleRect ("Twinkle Rect", Vector) = (0.30,0.38,0.82,0.80)
        _Hit0 ("Hit 0 (x, y, age, strength)", Vector) = (0,0,0,0)
        _Hit1 ("Hit 1", Vector) = (0,0,0,0)
        _Hit2 ("Hit 2", Vector) = (0,0,0,0)
        _Hit3 ("Hit 3", Vector) = (0,0,0,0)
        _HitStrength ("Hit Strength", Float) = 0.6
        _Rain ("Rain", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _PrevTex;
            sampler2D _SpecTex;
            float _Blend;
            float4 _UVScale;
            float4 _UVOffset;
            float _Zoom;
            float _Aspect;
            float _ScreenAspect;
            float _Pulse;
            float _Energy;
            float _HueShift;
            float _NeonBoost;
            float _FloorY;
            float _FloorWave;
            float _BeamStrength;
            float4 _BeamO0;
            float4 _BeamO1;
            float4 _BeamO2;
            float4 _BeamO3;
            float4 _BeamColA;
            float4 _BeamColB;
            float _Flicker;
            float _LaneCenter;
            float _LaneHalf;
            float _LaneSoft;
            float _LaneDim;
            float _Vignette;
            float _BarStrength;
            float _BarHeight;
            float _BarInner;
            float _ParticleStrength;
            float _Aberration;
            float4 _Focus;
            float4 _FocusCol;
            float _FocusStrength;
            float4 _Ripple;
            float4 _RippleCenter;
            float _Twinkle;
            float4 _TwinkleRect;
            float4 _Hit0;
            float4 _Hit1;
            float4 _Hit2;
            float4 _Hit3;
            float _HitStrength;
            float _Rain;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 suv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.suv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // xoay mau (hue) trong khong gian YIQ
            float3 hueShift(float3 c, float h)
            {
                float cs = cos(h);
                float sn = sin(h);
                float y = dot(c, float3(0.299, 0.587, 0.114));
                float i = dot(c, float3(0.596, -0.274, -0.322));
                float q = dot(c, float3(0.211, -0.523, 0.312));
                float i2 = i * cs - q * sn;
                float q2 = i * sn + q * cs;
                return float3(
                    y + 0.956 * i2 + 0.621 * q2,
                    y - 0.272 * i2 - 0.647 * q2,
                    y - 1.105 * i2 + 1.702 * q2);
            }

            // lay mau anh nen (co crossfade khi doi theme). Dung tex2Dlod de dung duoc trong nhanh re
            float3 sampleBg(float2 uv)
            {
                float3 c1 = tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb;
                if (_Blend >= 0.999) return c1;
                float3 c0 = tex2Dlod(_PrevTex, float4(uv, 0, 0)).rgb;
                return lerp(c0, c1, smoothstep(0.0, 1.0, _Blend));
            }

            // tia den san khau quet qua lai, bat dau tu vi tri den (toa do image uv)
            float3 beam(float2 uv, float4 o, float ang, float3 c)
            {
                float2 p = (uv - o.xy) * float2(_Aspect, 1.0);
                float2 d = float2(sin(ang), -cos(ang));
                float along = dot(p, d);
                float perp = abs(p.x * d.y - p.y * d.x);
                float width = 0.012 + 0.07 * max(along, 0.0);
                float core = exp(-(perp * perp) / (width * width)) * (0.02 / width);
                float fade = smoothstep(0.0, 0.05, along) * exp(-along * 1.6);
                return c * core * fade;
            }

            float4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 suv = i.suv;

                // uv cua anh: cover-fit + zoom + lac nhe
                float2 uv = (suv - 0.5) * _UVScale.xy / _Zoom + 0.5 + _UVOffset.xy;

                // vung giua man hinh (noi co lane/note)
                float lm = 1.0 - smoothstep(_LaneHalf, _LaneHalf + _LaneSoft, abs(suv.x - _LaneCenter));

                // song xung kich: vong tron lan ra tu giua, lam mep anh bi uon cong
                float ripBand = 0.0;
                if (_Ripple.y > 0.001)
                {
                    float2 rp = (suv - _RippleCenter.xy) * float2(_ScreenAspect, 1.0);
                    float rd = length(rp);
                    float rx = (rd - _Ripple.x * 1.6) / 0.07;
                    ripBand = exp(-rx * rx) * (1.0 - _Ripple.x) * _Ripple.y;
                    uv += (rp / max(rd, 0.0001)) * ripBand * 0.012;
                }

                // mat san: song nuoc lan tren phan phan chieu
                float fm = 1.0 - smoothstep(_FloorY - 0.10, _FloorY, uv.y);
                float wave = _FloorWave * fm * (1.0 + _Energy * 2.0);
                uv.x += sin(uv.y * 70.0 + t * 2.0) * wave;
                uv.y += sin(uv.x * 45.0 - t * 1.5) * wave * 0.5;

                // lech mau RGB theo beat: manh o hai ben, giu vung lane sach
                float2 rad = suv - 0.5;
                float edgeF = saturate(length(rad) * 1.6);
                float ca = _Aberration * (_Pulse + 0.15 * _Energy + ripBand) * (0.25 + edgeF);
                float3 col;
                if (ca > 0.0002)
                {
                    float2 co = normalize(rad + 0.0001) * ca;
                    col = float3(sampleBg(uv + co).r, sampleBg(uv).g, sampleBg(uv - co).b);
                }
                else
                {
                    col = sampleBg(uv);
                }

                col = hueShift(col, _HueShift);

                // neon = cac diem rat sang va bao hoa mau -> chop theo beat
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                float mx = max(col.r, max(col.g, col.b));
                float neon = smoothstep(0.75, 1.0, mx) * smoothstep(0.30, 0.50, lum);
                col += col * neon * _NeonBoost * (_Pulse + 0.25 * _Energy);

                // den neon nhap nhay ngau nhien
                float n = hash21(floor(uv * float2(16.0, 9.0)) + floor(t * 6.0));
                col -= col * neon * step(0.965, n) * _Flicker;

                // cua so toa nha lap loe (chi trong vung twinkleRect)
                if (_Twinkle > 0.001)
                {
                    float wh = hash21(floor(uv * float2(150.0, 84.0)) + floor(t * 3.0) * 7.0);
                    float inRect = step(_TwinkleRect.x, uv.x) * step(uv.x, _TwinkleRect.z)
                                 * step(_TwinkleRect.y, uv.y) * step(uv.y, _TwinkleRect.w);
                    float win = step(0.90, wh) * smoothstep(0.10, 0.25, lum) * (1.0 - smoothstep(0.55, 0.80, lum));
                    col += col * win * inRect * _Twinkle * (1.2 + _Energy * 2.0);
                }

                // anh sang lap lanh tren mat san
                col *= 1.0 + fm * 0.12 * sin(uv.x * 80.0 + t * 3.0) * (_Energy + 0.2);

                // do sang chung nhay nhe theo beat
                col *= 1.0 + 0.10 * _Pulse;

                // quang sang quanh mat trang / mat troi
                float2 fp = (uv - _Focus.xy) * float2(_Aspect, 1.0);
                float fg = exp(-dot(fp, fp) / max(_Focus.z * _Focus.z, 0.0001));
                col += _FocusCol.rgb * fg * _FocusStrength * (0.35 + 0.8 * _Energy + 0.8 * _Pulse);

                // tia den san khau
                float bs = _BeamStrength * (0.45 + _Energy + 0.7 * _Pulse);
                float3 b = float3(0, 0, 0);
                b += beam(uv, _BeamO0, 0.30 * sin(t * 0.9 + 0.0) + 0.10, _BeamColA.rgb);
                b += beam(uv, _BeamO1, 0.35 * sin(t * 0.7 + 1.7) - 0.05, _BeamColB.rgb);
                b += beam(uv, _BeamO2, 0.35 * sin(t * 0.8 + 3.1) + 0.05, _BeamColA.rgb);
                b += beam(uv, _BeamO3, 0.30 * sin(t * 1.0 + 4.6) - 0.10, _BeamColB.rgb);
                col += b * bs;

                // anh sang cua vong song xung kich
                col += lerp(_BeamColA.rgb, _BeamColB.rgb, 0.5) * ripBand * 0.8;

                // lam toi vung giua man hinh de gameplay de nhin
                col *= 1.0 - _LaneDim * lm;

                // phan ung khi danh trung note: vong song tren san + tru sang vuon len tu diem danh
                [unroll]
                for (int hi = 0; hi < 4; hi++)
                {
                    float4 hv = (hi == 0) ? _Hit0 : ((hi == 1) ? _Hit1 : ((hi == 2) ? _Hit2 : _Hit3));
                    if (hv.w > 0.001)
                    {
                        float2 dp = (suv - hv.xy) * float2(_ScreenAspect, 1.0);
                        float age = hv.z;
                        float fadeH = (1.0 - age) * (1.0 - age);
                        float rr = length(float2(dp.x, dp.y * 3.0));
                        float rx2 = (rr - age * 0.35) / 0.02;
                        float ringH = exp(-rx2 * rx2) * (1.0 - age);
                        float upH = suv.y - hv.y;
                        float pxn = dp.x / 0.035;
                        float pillar = exp(-pxn * pxn) * smoothstep(-0.02, 0.02, upH) * exp(-max(upH, 0.0) * 5.0) * fadeH;
                        float3 hc = lerp(_BeamColA.rgb, _BeamColB.rgb, saturate(hv.x));
                        col += hc * (ringH * 0.9 + pillar * 0.8) * hv.w * _HitStrength;
                    }
                }

                // equalizer hai ben (toa do man hinh, nam ngoai vung lane)
                float sd = abs(suv.x - 0.5);
                if (_BarStrength > 0.001 && sd > _BarInner)
                {
                    float u = (sd - _BarInner) / max(0.5 - _BarInner, 0.001);   // 0 sat lane, 1 ngoai mep
                    float idx = (1.0 - u) * 16.0;                               // bass o ngoai mep
                    float band = min(floor(idx), 15.0);
                    float fr = idx - band;
                    float h = tex2Dlod(_SpecTex, float4((band + 0.5) / 16.0, 0.5, 0, 0)).r * _BarHeight + 0.015;
                    float gap = smoothstep(0.06, 0.16, fr) * smoothstep(0.94, 0.84, fr);
                    float y = suv.y;
                    float inBar = smoothstep(h, h - 0.012, y);
                    float grad = 0.4 + 0.6 * saturate(y / max(h, 0.001));
                    float cap = smoothstep(0.012, 0.0, abs(y - h));
                    float3 bc = lerp(_BeamColA.rgb, _BeamColB.rgb, band / 15.0);
                    float fadeIn = smoothstep(0.0, 0.15, u);
                    col += bc * gap * (inBar * grad * 0.6 + cap * 1.2) * fadeIn * _BarStrength;
                }

                // hat sang troi len (3 lop, lop xa nho va cham hon)
                if (_ParticleStrength > 0.001)
                {
                    float3 ps = float3(0, 0, 0);
                    [unroll]
                    for (int L = 0; L < 3; L++)
                    {
                        float fl = (float)L;
                        float dens = 9.0 + fl * 6.0;
                        float2 g = float2(suv.x * _ScreenAspect, suv.y) * dens;
                        g.y -= t * (0.15 + 0.10 * fl);
                        float2 cell = floor(g);
                        float2 f = frac(g);
                        float h1 = hash21(cell + fl * 17.0);
                        float h2 = hash21(cell.yx + 5.3 + fl * 3.0);
                        float2 pos = float2(0.2 + 0.6 * h1, 0.2 + 0.6 * h2);
                        pos.x += 0.15 * sin(t * 0.8 + h1 * 6.28);
                        float star = smoothstep(0.06 + 0.05 * h2, 0.0, length(f - pos));
                        float tw = 0.5 + 0.5 * sin(t * (2.0 + 3.0 * h1) + h2 * 6.28);
                        float keep = step(0.55, h1);
                        ps += lerp(_BeamColA.rgb, _BeamColB.rgb, h2) * star * tw * keep * (1.0 - 0.25 * fl);
                    }
                    col += ps * _ParticleStrength * (0.5 + _Energy + _Pulse) * (1.0 - 0.7 * lm);
                }

                // mua roi (chi hien khi theme bat Rain)
                if (_Rain > 0.001)
                {
                    float rcx = floor(suv.x * 90.0);
                    float rnd = hash21(float2(rcx, 3.1));
                    float ry = frac(suv.y + t * (0.7 + rnd * 0.8) + rnd * 7.0);
                    float rxf = frac(suv.x * 90.0);
                    float rln = smoothstep(0.10, 0.0, abs(rxf - 0.5));
                    float drop = exp(-ry * 14.0) * step(0.62, rnd);
                    col += float3(0.55, 0.75, 1.0) * rln * drop * _Rain * 0.5 * (1.0 - 0.8 * lm);
                }

                // vignette
                float2 q = suv - 0.5;
                col *= 1.0 - _Vignette * dot(q, q) * 2.0;

                return float4(max(col, 0.0), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
