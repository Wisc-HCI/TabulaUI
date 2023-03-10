using UnityEngine;
using System.Collections.Generic;
using System;

/// Code written by thomas-allen (see https://github.com/thomas-allen/curve-fitter)
///  The code in this file was written by thomas-allen. 
///  The URL to the repository is https://github.com/thomas-allen/curve-fitter
///  The URL to the code below is https://github.com/thomas-allen/curve-fitter/blob/main/BezierCurve.cs
///  The relevant commit hash is 61f0024ec870b03289a1f04b49ead62c9bd8ecd0
///
///  The original code is licensed under the MIT license. The license can be
///  found in the third_party_licenses.txt under "curve-fitter".
class BezierCurve {
    private List<Vector3> controlPoints;
    private int numControlPoints;
    private Degree degree;

    public enum Degree : int {
        Cubic = 3
    };

    public BezierCurve(Degree degree) {
        this.degree = degree;
        numControlPoints = (int)degree + 1;
        controlPoints = new List<Vector3>(new Vector3[numControlPoints]);
    }

    public void SetControlPoint(int idx, Vector3 pos) {
        if (idx < 0 || idx >= numControlPoints) {
            throw new ArgumentException("Control point index  " + '\"' + idx + '\"'  + " out of bounds.");
        }

        controlPoints[idx] = pos;
    }

    public Vector3 GetControlPoint(int idx) {
        return controlPoints[idx];
    }

    public List<Vector3> GetControlPoints() {
        return new List<Vector3>(controlPoints);
    }

    public static float CubicB0(float t) {
        return Mathf.Pow(1f - t, 3);
    }

    public static float CubicB1(float t) {
        return 3 * t * Mathf.Pow(1f - t, 2);
    }

    public static float CubicB2(float t) {
        return 3 * t * t * (1f - t);
    }

    public static float CubicB3(float t) {
        return Mathf.Pow(t, 3);
    }
}