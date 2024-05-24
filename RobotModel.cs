using System;
using System.Collections.Specialized;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Gridrendering;
class Coord
{
    public double X{get; set;}
    public double Y{get; set;}
    public Coord Compl{private set{} get => new(){X = Y, Y = -X};}
    
    public Coord Copy{private set{} get => new(){X = this.X, Y = this.Y};}
    public Coord Affin(Coord E, double t) => this.Copy + E * t;

    public override string ToString() => $"({X}; {Y})";
    public double Norm{get => Math.Sqrt(X * X + Y * Y);}
    public static Coord operator +(Coord A, Coord B) => new(){X = A.X + A.Y, Y = A.Y + B.Y};
    public static Coord operator *(Coord A, double scal) => new(){X = A.X * scal, Y = A.Y * scal};
    public static double operator *(Coord A, Coord B) => A.X * B.X + A.Y * B.Y;
    public static Coord operator -(Coord A, Coord B) => A + (B * (-1));
    public double Atan2{private set{} get => Math.Atan(X == 0.0 ? -1000 : Y / X);}
}

class Hand
{
    public Coord[]? HandLine{get; set;} //задаёт направление плеча
    public Coord? HandPos{get; set;}
    public double HandLen{get; set;}
    
    //public double HandLen{get => (HandLine[0] - HandLine[1]).Norm;} //УТОЧНИТЬ ПО ПОВОДУ ПОЛУЧЕНИЯ ДЛИНЫ ПЛЕЧА РОБОТАК!!!
    public override string ToString() => $"Angle = {180 * (HandLine[0] - HandLine[1]).Atan2 / Math.PI}; Pos = {HandPos}; Length = {HandLen}";

    public bool CheckHand(double Lmin, double Lmax)
    {
        var LenAbs = Math.Abs(HandLen);
        return LenAbs > Lmin && LenAbs < Lmax;
    }
}

class GridContext //вспомогательный класс, имплементирующий сетку
{
    public Coord G0{get; set;}
    public Coord G1{get; set;}
    public Coord Step{get;set;}
}    

class RobotState
{
    public Coord? F2{get; set;}
    public Hand? F1{get; set;}
    public Hand? F0{get; set;}
    public Hand? H{get; set;}
    public Coord? Center{get; set;}
    public RobotState Copy
    {
        private set{}
        get => new(){F2 = F2, Center = Center, F1 = F1, F0 = F0, H = H};
    }
}

class RobotModel
{
    private GridContext GridCtx = new(){G0 = new(){X = 0, Y = 30}, G1 = new(){X = 0, Y = 30}, Step = new(){X = 0.0305, Y = 0.0165}};
    private double robRad = 0.5 * 0.152;
    private double robF1A = Math.PI * 101.4 / 180;
    private double robHx = 0.031;
    private double robHy = 0.044; 
    private double robF0x = 0.041;  
    private double robF1x = 0.041; 
    private double robC1x = 0.015; 
    private double robC1y = 0.0745; 
    private double robF2x = 0.025; 
    private double robF2y = 0.0278; 
    private double robC2x = 0.018; 
    private double robC2y = 0.046; 
    private double robL1Min = 0.0593;
    private double robL1Max = 0.01838;
    private double robL0Min = 0.0623;
    private double robL0Max = 0.2696;

    private double AngNorm(double AngValue) => AngValue < 0 ? 2 * Math.PI + AngValue : AngValue; //вспомогательная функция модуля угла
    
    private Coord Ein(double alpha) => new(){X = Math.Cos(alpha), Y = Math.Sin(alpha)};

    private Coord EinNeg(double alpha) => Ein(alpha) * -1;

    //функции проверок на возможность существования задаваемых параметров плечей 
    private bool CheckAngle(Coord Center, Hand Hand0, Hand Hand1)
    {
        var alpha = AngNorm((Hand0.HandLine[1] - Center).Atan2);
        var beta = AngNorm((Hand1.HandLine[1] - Center).Atan2);
        return alpha - beta > 3 * Math.PI / 4 && beta - alpha < 5 * Math.PI / 4;
    }

    private bool CheckHand(Hand SomeHand, string HandID)
    {
        if(HandID == "H0")
            return SomeHand.CheckHand(robL0Min, robL0Max);
        else if(HandID == "H1")
            return SomeHand.CheckHand(robL1Min, robL1Max);
        return false;
    }
    private bool CheckFinger(RobotState CurrRobotState, string FingerID)
    {
        if(FingerID == "F0")
            return CheckHand(CurrRobotState.F0, "H0");
        else if(FingerID == "F1")
            return CheckHand(CurrRobotState.F1, "H1");
        return false;
    }

    private bool CheckH(RobotState CurrRobotState)
    {
        var ba = CheckAngle(CurrRobotState.Center, CurrRobotState.F0, CurrRobotState.H);
        var br = CurrRobotState.H.CheckHand(robHx + robL1Min, robHy + robL1Max); 
        return ba && br;                                                                   
    }

    public RobotState? Inversion02(Coord ij0, Coord ij2) //ПРОВЕРИТЬ В ЭТОЙ ФУНКЦИИ ПРАВИЛЬНОСТЬ ПОРЯДКА СЛЕДОВАНИЯ ИНДЕКСНЫХ КОЭФ-ОВ!!!
    {
        var S_x = GridCtx.Step.X;
        var S_y = GridCtx.Step.Y;
        var Fing2_x = robF2x;
        var Fing2_y = robF2y;
        var Fing0_x = robF0x;
        var C2_y = robC2y;
        var C2_x = robC2x;
        
        var x2 = S_x * ij2.Y;
        var x0 = S_x * ij0.Y;
        var y2 = S_y * ij2.X;
        var y0 = S_y * ij0.X;
        double dx = x0 - x2, dy = y0 - y2;
        if(ij2.X > ij0.X) //выяснить насчёт сигнатуры функции обратной кинематической задачи в 180-й строке
        {
            dx = -dx;
            dy = -dy;
        }
        var D_xy = new Coord(){X = dx, Y = dy};
        var d = D_xy.Norm;
        var theta = D_xy.Atan2;
        var alpha = Math.Acos((Fing0_x - Fing2_x) / d);

        if(theta is not double.NaN)
        {
            var e0 = Ein(theta - alpha);
            double l0;
            Coord e1, q2, c, q0;
            Hand h0;
            if(ij2.X <= ij0.X)
            {
                l0 = e0 * D_xy - Fing2_y;
                e1 = e0.Compl;
                q2 = new Coord(){X = x2, Y = y2}.Affin(e1, Fing2_x);
                c = q2.Affin(e0, -C2_y).Affin(e1, C2_x); //c = aff e1 c2x . aff e0 (-c2y) $ q2 ---- 211 строчка в файле Пасфайндер
                q0 = q2.Affin(e0, Fing2_x);
                h0 = new Hand(){HandLine = [e0, q0], HandPos = new(){X = x0, Y = y0}, HandLen = l0};
            }
            else
            {
                l0 = e0 * D_xy;
                e1 = e0.Compl;
                q2 = new Coord(){X = x2, Y = y2}.Affin(e1, -Fing2_x);
                c = q2.Affin(e0, C2_y).Affin(e1, -C2_x); //c = aff e1 -c2x . aff e0 c2y $ q2 ---- 246 строчка в файле Пасфайндер
                q0 = q2.Affin(e0, Fing2_x);
                h0 = new Hand(){HandLine = [e0, q0], HandPos = new(){X = x0, Y = y0}, HandLen = -l0};
            }
            return new RobotState(){F2 = new(){X = x2, Y = y2}, F0 = h0, Center = c};
        }
        else return null;
    }

    public RobotState? Inversion1(Coord ij1, RobotState CurrRobotState)
    {
        var S_x = GridCtx.Step.X;
        var S_y = GridCtx.Step.Y;
        var Fing1_x = robF1x;
        var C1_y = robC1y;
        var C1_x = robC1x;

        var C = CurrRobotState.Center;
        var x1 = S_x * ij1.Y;
        var y1 = S_y * ij1.X;
        if(y1 < C.Y)
        {
            var z = C1_x + Fing1_x;
            var dx = C.X - x1;
            var dy = C.Y - y1;
            var D_xy = new Coord(){X = dx, Y = dy};
            var d = D_xy.Norm;
            var beta = Math.Acos(dx / d);
            var alpha = Math.Asin(z / d);
            if(beta is double.NaN || alpha is double.NaN) return null;
            else
            {
                var phi = alpha + beta;
                var e0 = Ein(phi);
                var e1 = e0.Compl;
                var q0 = C.Affin(e1, -C1_x).Affin(e0, -C1_y);
                var l1 = D_xy * e0 - C1_y;
                var h = new Hand(){HandLine = [e0, e1], HandPos = new(){X = x1, Y = y1}, HandLen = -l1};
                var NewRobotState = CurrRobotState.Copy;
                NewRobotState.F1 = h;
                return NewRobotState;
            }
        }
        else
        {
            var z = C1_x + Fing1_x;
            var dx = x1 - C.X;
            var dy = y1 - C.Y;
            var D_xy = new Coord(){X = dx, Y = dy};
            var d = D_xy.Norm;
            var beta = Math.Acos(dx / d);
            var alpha = Math.Asin(z / d);
            if(beta == double.NaN || alpha == double.NaN) return null;
            else
            {
                var phi = alpha + beta;
                var e0 = Ein(phi);
                var e1 = e0.Compl;
                var q0 = C.Affin(e1, C1_x).Affin(e0, C1_y);
                var l1 = D_xy * new Coord(){X = x1, Y = y1} - C1_y;
                var h = new Hand(){HandLine = [e0, q0], HandPos = new(){X = x1, Y = y1}, HandLen = l1};
                var NewRobotState = CurrRobotState.Copy;
                NewRobotState.F1 = h;
                return NewRobotState;
            }
        }
    }

    public RobotState? InversionH(Coord ij1, RobotState CurrRobotState)
    {
        var S_x = GridCtx.Step.X;
        var S_y = GridCtx.Step.Y;
        var Fing1_x = robF1x;
        var C1_y = robC1y;
        var C1_x = robC1x;

        var hy = robHy;
        var hx = robHx;
        var C = CurrRobotState.Center;
        var x1 = S_x * ij1.Y;
        var y1 = S_y * ij1.X;

        if(y1 < C.Y)
        {
            var z = C1_x + hx;
            var dx = C.X - x1;
            var dy = C.Y - y1;
            var D_xy = new Coord(){X = dx, Y = dy};
            var d = D_xy.Norm;
            var beta = Math.Acos(dx / d);
            var alpha = Math.Asin(z / d); 
            if(beta == double.NaN || alpha == double.NaN) return null;
            else
            {
                var phi = alpha + beta;
                var e0 = Ein(phi);
                var e1 = e0.Compl;
                var q0 = C.Affin(e1, -C1_x).Affin(e0, -C1_y);
                var l1 = D_xy * e0 - C1_y;
                var hh = new Hand(){HandLine = [e0, q0], HandPos = new Coord(){X = x1, Y = y1}, HandLen = -l1};
                var q1 = q0.Affin(e0, -l1 + hy).Affin(e1, -Fing1_x);
                var h1 = new Hand(){HandLine = [e0, q0], HandPos = q1, HandLen = -l1 + hy};
                var NewRobotState = CurrRobotState.Copy;
                NewRobotState.H = hh;
                NewRobotState.F1 = h1;
                return NewRobotState;
            }
        }
        else
        {
            var z = C1_x + hx;
            var dx = x1 - C.X;
            var dy = y1 - C.Y;
            var D_xy = new Coord(){X = dx, Y = dy};
            var d = D_xy.Norm;
            var beta = Math.Acos(dx / d);
            var alpha = Math.Asin(z / d);
            if(beta == double.NaN || alpha == double.NaN) return null;
            else
            {
                var phi = alpha + beta;
                var e0 = Ein(phi);
                var e1 = e0.Compl;
                var q0 = C.Affin(e0, C1_y).Affin(e1, C1_x);
                var l1 = D_xy * e0 - C1_y;
                var hh = new Hand(){HandLine = [e0, q0], HandPos = new Coord(){X = x1, Y = y1}, HandLen = -l1};
                var q1 = q0.Affin(e0, l1 - hy).Affin(e1, Fing1_x);
                var h1 = new Hand(){HandLine = [e0, q0], HandPos = q1, HandLen = l1 - hy};
                var NewRobotState = CurrRobotState.Copy;

                NewRobotState.H = hh;
                NewRobotState.F1 = h1;
                return NewRobotState;
            }
        }
    }

    //РЕАЛИЗОВАТЬ В ЭТОМ МЕСТЕ ОБРАБОТКУ ИСКЛЮЧЕНИЙ ПРИ ПОПАДАНИИ РОБОТА В НЕВОЗМОЖНОЕ СОСТОЯНИЕ 
    public RobotState? Inversion(Hand F2, Hand F0, Hand F1)
    {
        var NewRobotState = Inversion02(F2.HandPos, F0.HandPos);
        if(NewRobotState is not null)
        {
            if(CheckFinger(NewRobotState, "F0"))
            {
                NewRobotState = Inversion1(F1.HandPos, NewRobotState);
                if(NewRobotState is not null && CheckFinger(NewRobotState, "F1"))
                    return NewRobotState;
                else return null;
            }
            else return null;
        }
        return null;
    }
    
    //isAccessibleF1 grid rob f2 f0 f1 = inversion grid rob f2 f0 f1 >>= (return . isJust)
    public bool IsAccessible(int[] f0, int[] f1, int[] f2, RobotState CurrRobotState)
    {
        var NewRobotState = Inversion02(new(){X = f2[0], Y = f2[1]}, new(){X = f0[0], Y = f0[1]});
        if(NewRobotState is not null)
        {
            if(CheckFinger(NewRobotState, "F0"))
            {
                NewRobotState = InversionH(new(){X = f1[0], Y = f1[1]}, NewRobotState);
                if(NewRobotState is not null) return CheckH(NewRobotState);
                else return false;
            }
            else return false;
        }
        return false;
    }
}