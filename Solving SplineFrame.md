For the SplineFrame it is true that `splineFrame(a+x, b+y)[z]` = `splineFrame(a-x, b-y)[z]`.
This can be exploited to find an intersection.
To do the intersection, set a and b equal, and set x=0 and y=0.

Then this becomes a simple spline interpolation. From the SplineFrame, the formula for a new spline is:
splineFrame(a, a) = O + X*a.x + Y*a.y + Z*a.z. Since a.z=0, we're not reviewing other splines that go through the same point.

The equation system becomes:
```
    u = 1-z
    uu = u*u
    zz = z*z
    basis1 = u * uu
    basis2 = z * uu
    basis3 = u * zz
    basis4 = z * zz

    spline = O + X*x + Y*y
    P = spline.a * basis1 + spline.b * basis2 + spline.c * basis3 + spline.d * basis4 
```

After some substitution, it becomes:
```
    spline = O + X*x + Y*y
    P = spline.a *   -z^3 + 3*z^2 - 3*z + 1
      + spline.b *  3*z^3 - 6*z^2 + 3*z
      + spline.c * -3*z^3 + 3*z^2
      + spline.d *    z^3
```
Substituting spline components, it becomes:
```
    A = O.a + X.a*x + Y.a*y
    B = O.b + X.b*x + Y.b*y
    C = O.c + X.c*x + Y.c*y
    D = O.d + X.d*x + Y.d*y
    P = A *   -z^3 + 3*z^2 - 3*z + 1
      + B *  3*z^3 - 6*z^2 + 3*z
      + C * -3*z^3 + 3*z^2
      + D *    z^3
```
```
    P = (O.a + X.a*x + Y.a*y) * (  -z^3 + 3*z^2 - 3*z + 1)
      + (O.b + X.b*x + Y.b*y) * ( 3*z^3 - 6*z^2 + 3*z    )
      + (O.c + X.c*x + Y.c*y) * (-3*z^3 + 3*z^2          )
      + (O.d + X.d*x + Y.d*y) * (   z^3                  )
```
Transposing the polynomial:
```
    P = 
      (-O.a - X.a*x - Y.a*y + 3*O.b + 3*X.b*x + 3*Y.b*x - 3*O.c - 3*X.c*x - 3*Y.c*y + O.d + X.d*x + Y.d*y) * z^3 +
      (3*O.a + 3*X.a*x + 3*Y.a*y - 6*O.b - 6*X.b*x - 6*Y.b*y + 3*O.c + 3*X.c*x + 3*Y.c*y) * z^2 +
      (-3*O.a -3*X.a*x -3*Y.a*y +3*O.b +3*X.b*x +3*Y.b*y) * z +
      (O.a + X.a*x + Y.a*y)
```

With matrices, it becomes:
```
    [1, z, z^2, z^3]
  * [
       1,  0,  0, 0
      -3,  3,  0, 0
       3, -6,  3, 0
      -1,  3, -3, 1
    ]
   * O + X*x + Y*y
```
```
    [1, z, z^2, z^3]
  * [
       1,  0,  0, 0
      -3,  3,  0, 0
       3, -6,  3, 0
      -1,  3, -3, 1
    ]
   * O + X*x + Y*y = p
```
```
    [1, z, z^2, z^3]
  * [
       1,  0,  0, 0
      -3,  3,  0, 0
       3, -6,  3, 0
      -1,  3, -3, 1
    ]
   * O + X*x + Y*y = p
```