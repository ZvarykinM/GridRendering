using System;
using System.Collections.Generic;

namespace GridRendering;

class Robot
{
    //параметры трубной доски, задаются роботу при инициализации
    public double StepX{get; set;}
    public double StepY{get; set;}
    public double Radius{get; set;}

    // private const double PhiMin = 70 * Math.PI / 180;
    // private const double PhiMax = 140 * Math.PI / 180;
    // private const double L0Min = 0.5;
    // private const double L0Max = 20;
    // private const double L1Min = 0.5;
    // private const double L1Max = 20;
    public int[] F0Current{get; set;}
    public int[] F1Current{get; set;}
    public int[] F2Current{get; set;}
    public Dictionary<int[], string> F0PossibleStates{get; set;}
    public Dictionary<int[], string> F1PossibleStates{get; set;}
    public Dictionary<int[], string> F2PossibleStates{get; set;} 

    // public void Renew()
    // {
    //     double[] dataX = [F0Current[0] * StepX, F2Current[0] * StepX, F1Current[0] * StepX];
    //     double[] dataY = [F0Current[1] * StepY, F2Current[1] * StepY, F1Current[1] * StepY];


    // }


}