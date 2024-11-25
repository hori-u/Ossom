/*
XorShift.cs 乱数ライブラリ
coded by isaku@pb4.so-net.ne.jp
*/

using System;

class XorShift
{
    uint   x,y,z,w;     /* 状態テーブル */
    int    range;       /* NextIntEx で前回の範囲 */
    uint   bse;         /* NextIntEx で前回の基準値 */
    int    shift;       /* NextIntEx で前回のシフト数 */
    int    normal_sw;   /* NextNormal で残りを持っている */
    double normal_save; /* NextNormal の残りの値 */

    /* 初期化のお手伝い */
    uint InitMtSub(uint s,uint i)
    { unchecked { return 1812433253*(s ^ (s >> 30)) + i; } }

    /* 整数の種 s による初期化 */
    public void InitMt(uint s) {
        x=InitMtSub(s, 0);
        y=InitMtSub(x, 1); 
        z=InitMtSub(y, 2); 
        w=InitMtSub(z, 3);
        range=0; 
        normal_sw=0;
    }

    public XorShift(uint s) { InitMt(s); }

    /* 配列 init_key による初期化 */
    public void InitMtEx(uint[]key) {
        uint i,k,s,len=(uint)key.Length;
        uint[]v=new uint[4];

        InitMt(1);
        v[0] = x;
        v[1] = y;
        v[2] = z;
        v[3] = w;
        s    = w;
        for (i = 0; i < len; i++)
            v[i & 3] ^= s = InitMtSub(s, key[i] + i);
        for (k = 0; k < 3; k++, i++)
            v[i & 3] ^= s = InitMtSub(s, k);
        x = v[0];
        y = v[1];
        z = v[2];
        w = v[3];
        if (x == 0 && y == 0 && z == 0 && w == 0)
            x = 1;
    }

    public XorShift(uint[]key){
        InitMtEx(key);
    }

    /* 32ビット符号なし整数の乱数 */
    public uint NextMt(){
        uint t = x ^ (x << 11);
        x = y;
        y = z;
        z = w;
        return w = w ^ (w >> 19) ^ t ^ (t >> 8);
    }

    /* ０以上 n 未満の整数乱数 */
    public int NextInt(int n){
        return (int)(n * (1.0 / 4294967296.0) * NextMt());
    }

    /* ０以上１未満の乱数(53bit精度) */
    public double NextUnif() {
        uint z = NextMt() >> 11, y = NextMt();
        return(y * 2097152.0 + z) * (1.0 / 9007199254740992.0);
    }

    /* 丸め誤差のない０以上 range_ 未満の整数乱数 */
    public int NextIntEx(int range_){
        uint y_, base_, remain_;
        int shift_;

        if (range_ <= 0)
            return 0;
        if (range_!=range){
            bse = (uint)(range=range_);
            for (shift = 0; bse <= (1UL << 30); shift++){
                bse <<= 1;
            }
        }
        for (;;) {
            y_ = NextMt() >> 1;
            if (y_ < bse)
                return (int)(y_>>shift);
            base_   = bse;
            shift_  = shift;
            y_     -= base_;
            remain_ =(1U << 31) - base_;

            for ( ; remain_ >= (uint)range_; remain_ -= base_) {
                for ( ; base_ > remain_; base_ >>= 1)
                    shift_--;
                if (y_ < base_)
                    return (int)(y_ >> shift_);
                else y_ -= base_;
            }
        }
    }

    /* 自由度νのカイ２乗分布 */
    public double NextChisq(double n) {
        return 2 * NextGamma(0.5 * n);
    }

    /* パラメータａのガンマ分布 */
    public double NextGamma(double a) {
        double t, u, X, Y;
        if (a > 1) {
            t = Math.Sqrt(2 * a - 1);
            do {
                do {
                    do {
                        X = 1 - NextUnif();
                        Y = 2 * NextUnif() - 1;
                    } while (X * X + Y * Y > 1);
                    Y /= X;
                    X = t * Y + a - 1;
                } while (X <= 0);
                u = (a - 1) * Math.Log(X / (a - 1)) - t * Y;
            } while (u <- 50 || NextUnif() > (1 + Y * Y) * Math.Exp(u));
        } else {
            t = 2.718281828459045235 / (a + 2.718281828459045235);
            do {
                if (NextUnif() < t) {
                    X = Math.Pow(NextUnif(),1 / a); Y = Math.Exp(-X);
                } else {
                    X = 1 - Math.Log(1 - NextUnif());
                    Y =     Math.Pow(X, a - 1);
                }
            } while (NextUnif() >= Y);
        }
        return X;
    }

    /* 確率Ｐの幾何分布 */
    public int NextGeometric(double p) {
        return (int) Math.Ceiling( Math.Log(1.0 - NextUnif()) / Math.Log(1 - p));
    }

    /* 三角分布 */
    public double NextTriangle() {
        double a = NextUnif(), b = NextUnif();
        return a - b;
    }

    /* 平均１の指数分布 */
    public double NextExp() {
        return - Math.Log(1 - NextUnif());
    }

    /* 標準正規分布(最大8.57σ) */
    public double NextNormal() {
        if (normal_sw == 0) {
            double t = Math.Sqrt(- 2 * Math.Log(1.0 - NextUnif()));
            double u = 3.141592653589793 * 2 * NextUnif();
            normal_save = t * Math.Sin(u);
            normal_sw =1;
            return t * Math.Cos(u);
        } else {
            normal_sw = 0;
            return normal_save;
        }
    }

    /* Ｎ次元のランダム単位ベクトル */
    public void NextUnitVect(out double[]v, int n) {
        int i;
        double r = 0;
        v = new double[n];
        for (i=0;i<n;i++) {
            v[i] = NextNormal();
            r   += v[i] * v[i];
        }
        r = Math.Sqrt(r);
        for (i=0;i<n;i++)
            v[i] /= r;
    }

    /* パラメータＮ,Ｐの２項分布 */
    public int NextBinomial(int n,double p) {
        int i,r = 0;
        for (i = 0; i < n; i++)
            if (NextUnif() < p)
                r++;

        return r;
    }

    /* 相関係数Ｒの２変量正規分布 */
    public void NextBinormal(double r, out double a, out double b) {
        double r1, r2, s;
        do {
            r1 = 2 * NextUnif() - 1;
            r2 = 2 * NextUnif() - 1;
            s  = r1 * r1 +r2 * r2;
        } while (s > 1 || s == 0);
        s  = - Math.Log(s) / s;
        r1 =   Math.Sqrt((1 + r) * s) * r1;
        r2 =   Math.Sqrt((1 - r) * s) * r2;
        a = r1 + r2;
        b = r1 - r2;
    }

    /* パラメータＡ,Ｂのベータ分布 */
    public double NextBeta(double a, double b) {
        double temp = NextGamma(a);
        return temp / (temp + NextGamma(b));
    }

    /* パラメータＮの累乗分布 */
    public double NextPower(double n) {
        return Math.Pow(NextUnif(), 1.0 / (n + 1));
    }

    /* ロジスティック分布 */
    public double NextLogistic() {
        double r;
        do r = NextUnif();
        while (r == 0);
        return Math.Log(r / (1 - r));
    }

    /* コーシー分布 */
    public double NextCauchy() {
        double a, b;
        do {
            a = 1 - NextUnif();
            b = 2 * NextUnif() - 1;
        } while (a * a + b * b > 1);
        return b / a;
    }

    /* 自由度 n1,n2 のＦ分布 */
    public double NextFDist(double n1, double n2) {
        double nc1 = NextChisq(n1);
        double nc2 = NextChisq(n2);
        return (nc1 * n2) / (nc2 * n1);
    }

    /* 平均λのポアソン分布 */
    public int NextPoisson(double lambda) {
        int k;
        lambda = Math.Exp(lambda) * NextUnif();
        for (k = 0; lambda > 1; k++)
            lambda *= NextUnif();
        return k;
    }

    /* 自由度Ｎのｔ分布 */
    public double NextTDist(double n) {
        double a, b, c;
        if (n <= 2) {
            do a = NextChisq(n);
            while (a == 0);
            return NextNormal() / Math.Sqrt(a / n);
        } do {
            a = NextNormal();
            b = a * a / (n - 2);
            c = Math.Log(1 - NextUnif()) / (1 - 0.5 * n);
        } while (Math.Exp(- b - c) > 1 - b);
        return a / Math.Sqrt((1 - 2.0 / n) * (1 - b));
    }

    /* パラメータαのワイブル分布 */
    public double NextWeibull(double alpha) {
        return Math.Pow(- Math.Log(1 - NextUnif()),1 / alpha);
    }
}

class XorShift_m {
    static XorShift[] zz = {
        new XorShift(1),
        new XorShift(unchecked((uint) -  1)),
        new XorShift(unchecked((uint) -  2)),
        new XorShift(unchecked((uint) -  3)),
        new XorShift(unchecked((uint) -  4)),
        new XorShift(unchecked((uint) -  5)),
        new XorShift(unchecked((uint) -  6)),
        new XorShift(unchecked((uint) -  7)),
        new XorShift(unchecked((uint) -  8)),
        new XorShift(unchecked((uint) -  9)),
        new XorShift(unchecked((uint) - 10)),
        new XorShift(unchecked((uint) - 11)),
        new XorShift(unchecked((uint) - 12)),
        new XorShift(unchecked((uint) - 13)),
        new XorShift(unchecked((uint) - 14)),
        new XorShift(unchecked((uint) - 15)),
        new XorShift(unchecked((uint) - 16)),
        new XorShift(unchecked((uint) - 17)),
        new XorShift(unchecked((uint) - 18)),
        new XorShift(unchecked((uint) - 19)),
        new XorShift(unchecked((uint) - 20)),
        new XorShift(unchecked((uint) - 21)),
        new XorShift(unchecked((uint) - 22)),
        new XorShift(unchecked((uint) - 23)),
        new XorShift(unchecked((uint) - 24)),
        new XorShift(unchecked((uint) - 25)),
        new XorShift(unchecked((uint) - 26)),
        new XorShift(unchecked((uint) - 27)),
        new XorShift(unchecked((uint) - 28)),
        new XorShift(unchecked((uint) - 29)),
        new XorShift(unchecked((uint) - 30)),
        new XorShift(unchecked((uint) - 31))
    };

    public static void InitMt_m(int i, uint s) {
        zz[i].InitMt(s);
    }

    public static void InitMtEx_m(int i, uint[] k) {
        zz[i].InitMtEx(k);
    }

    public static void NextUnitVect_m(int i, out double[] v, int n) {
        zz[i].NextUnitVect(out v, n);
    }

    public static void NextBinormal_m(int i, double r, out double a, out double b) {
        zz[i].NextBinormal(r, out a, out b);
    }
    
    public static int NextInt_m(int i, int n) {
        return zz[i].NextInt(n);
    }

    public static int NextIntEx_m(int i, int r) {
        return zz[i].NextIntEx(r);
    }

    public static int NextPoisson_m(int i, double l) {
        return zz[i].NextPoisson(l);
    }

    public static int NextGeometric_m(int i, double p) {
        return zz[i].NextGeometric(p);
    }

    public static int NextBinomial_m(int i, int n, double p) {
        return zz[i].NextBinomial(n, p);
    }

    public static uint NextMt_m(int i) {
        return zz[i].NextMt();
    }

    public static double NextExp_m(int i) {
        return zz[i].NextExp();
    }

    public static double NextUnif_m(int i) {
        return zz[i].NextUnif();
    }

    public static double NextNormal_m(int i) {
        return zz[i].NextNormal();
    }

    public static double NextTriangle_m(int i) {
        return zz[i].NextTriangle();
    }

    public static double NextChisq_m(int i,double n) {
        return zz[i].NextChisq(n);
    }

    public static double NextGamma_m(int i,double a) {
        return zz[i].NextGamma(a);
    }

    public static double NextBeta_m(int i,double a,double b) {
        return zz[i].NextBeta(a,b);
    }

    public static double NextPower_m(int i,double n) {
        return zz[i].NextPower(n);
    }

    public static double NextLogistic_m(int i) {
        return zz[i].NextLogistic();
    }

    public static double NextCauchy_m(int i) {
        return zz[i].NextCauchy();
    }

    public static double NextFDist_m(int i,double n1,double n2) {
        return zz[i].NextFDist(n1,n2);
    }

    public static double NextTDist_m(int i,double n) {
        return zz[i].NextTDist(n);
    }

    public static double NextWeibull_m(int i,double a) {
        return zz[i].NextWeibull(a);
    }
}

