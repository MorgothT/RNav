namespace GeoConverter
{
    public class Converter
    {
        public enum Ellipsoids { WGS_84, GRS_80 };
        public enum Projections { ITM, UTM_36N };

        public static string[] EllipsoidsNames
        {
            get { return new string[] { "WGS 84", "GRS 80 (05/12)" }; }
        }

        public static string[] ProjectionsNames
        {
            get { return new string[] { "ITM (05/12)", "UTM Z36 N" }; }
        }

        public Converter(Projections from, Projections to)
        {
            fromPr = from;
            toPr = to;
            mode = Mode.pr2pr;
        }
        public Converter(Projections from, Ellipsoids to)
        {
            fromPr = from;
            toEll = to;
            mode = Mode.pr2el;
        }
        public Converter(Ellipsoids from, Projections to)
        {
            fromEll = from;
            toPr = to;
            mode = Mode.el2pr;
        }
        public Converter(Ellipsoids from, Ellipsoids to)
        {
            fromEll = from;
            toEll = to;
            mode = Mode.el2el;
        }
        private readonly Converter.Ellipsoids fromEll, toEll;
        private readonly Converter.Projections fromPr, toPr;
        private enum Mode { pr2pr, el2pr, pr2el, el2el };
        private readonly Mode mode;

        // -------------------------
        public class Point3d
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public Point3d(double X, double Y, double Z)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }
            public override string ToString()
            {
                string retVal = string.Empty;

                retVal = string.Format("{0},{1},{2}", X.ToString(), Y.ToString(), Z.ToString());

                return retVal;
            }
        }
        // -------------------------

        public static Point3d ToRad(Point3d point)
        {
            return new Point3d(point.X * Math.PI / 180, point.Y * Math.PI / 180, point.Z);
        }
        public static Point3d ToDeg(Point3d point)
        {
            return new Point3d(point.X * 180 / Math.PI, point.Y * 180 / Math.PI, point.Z);
        }

        public Point3d Convert(Point3d point)
        {
            switch (mode)
            {
                case Mode.pr2pr: return Converter.Convert(fromPr, toPr, point);
                case Mode.pr2el: return ToDeg(Converter.Convert(fromPr, toEll, point));
                case Mode.el2pr: return Converter.Convert(fromEll, toPr, ToRad(point));
                case Mode.el2el: return ToDeg(Converter.Convert(fromEll, toEll, ToRad(point)));
            }
            return point;
        }

        private static Point3d Convert(Projections from, Projections to, Point3d point)
        {
            if (from == to) return point;
            Projection pTo = projections[(int)to];
            Projection pFrom = projections[(int)from];
            return pTo.FromGeo(Convert(pFrom.ellipsoid, pTo.ellipsoid, pFrom.ToGeo(point)));
        }
        private static Point3d Convert(Ellipsoids from, Projections to, Point3d point)
        {
            Projection pr = projections[(int)to];
            return pr.FromGeo(Convert(from, pr.ellipsoid, point));
        }
        private static Point3d Convert(Projections from, Ellipsoids to, Point3d point)
        {
            Projection pr = projections[(int)from];
            point = pr.ToGeo(point);
            return Convert(pr.ellipsoid, to, point);
        }
        private static Point3d Convert(Ellipsoids from, Ellipsoids to, Point3d point)
        {
            if (from == to) return point;
            return transformations[(int)from, (int)to].Transform(point);
        }

        private class Ellipsoid
        {
            public Ellipsoid(double a, double f)
            {
                this.a = a;
                this.f = f;
                b = a * (1 - f);
                e2 = f * (2 - f);
            }
            public readonly double a, b, f, e2;
        }
        private class Projection
        {
            public Projection(Ellipsoids e, double oLon, double oLat, double e0, double n0, double scale)
            {
                ellipsoid = e;
                Ellipsoid = ellipsoids[(int)e];
                lat0 = oLat;
                lon0 = oLon;
                E0 = e0;
                N0 = n0;
                k0 = scale;
                double e4 = Ellipsoid.e2 * Ellipsoid.e2;
                double e6 = e4 * Ellipsoid.e2;
                A0 = 1 - (Ellipsoid.e2 / 4) - (e4 * 3 / 64) - (e6 * 5 / 256);
                A2 = (Ellipsoid.e2 + e4 / 4 + e6 * 15 / 128) * 3 / 8;
                A4 = (e4 + e6 * 3 / 4) * 15 / 256;
                A6 = e6 * 35 / 3072;
                m0 = m(oLat);
                n = (Ellipsoid.a - Ellipsoid.b) / (Ellipsoid.a + Ellipsoid.b);
                double n2 = n * n;
                double n4 = n2 * n2;
                G = Ellipsoid.a * (1 - n) * (1 - n2) * (1 + n2 * 9 / 4 + n4 * 225 / 64) * (Math.PI / 180);
            }
            public double m(double phi)
            {
                double _2phi = phi + phi;
                double _4phi = _2phi + _2phi;
                double _6phi = _4phi + _2phi;
                return Ellipsoid.a * (A0 * phi - A2 * Math.Sin(_2phi) + A4 * Math.Sin(_4phi) - A6 * Math.Sin(_6phi));
            }
            public Point3d ToGeo(Point3d point)
            {
                double N_ = point.Y - N0;
                double n2 = n * n;
                double n3 = n2 * n;
                double n4 = n2 * n2;
                double m_ = m0 + N_ / k0;
                double sigma = m_ * Math.PI / (180 * G);
                double phi_ = sigma + (n * 3 / 2 - n3 * 27 / 32) * Math.Sin(2 * sigma)
                    + (n2 * 21 / 16 - n4 * 55 / 32) * Math.Sin(4 * sigma)
                    + (n3 * 151 / 96) * Math.Sin(6 * sigma) + n4 * 1097 / 512 * Math.Sin(8 * sigma);

                double sin_phi_ = Math.Sin(phi_);
                double tmp = 1 - Ellipsoid.e2 * sin_phi_ * sin_phi_;
                double rho_ = Ellipsoid.a * (1 - Ellipsoid.e2) / Math.Pow(tmp, 1.5);
                double nu_ = Ellipsoid.a / Math.Sqrt(tmp);
                double psi_ = nu_ / rho_;
                double psi2 = psi_ * psi_;
                double psi3 = psi2 * psi_;
                double psi4 = psi2 * psi2;
                double t_ = Math.Tan(phi_);
                double t2 = t_ * t_;
                double t4 = t2 * t2;
                double t6 = t4 * t2;
                double E_ = point.X - E0;
                double x = E_ / (k0 * nu_);
                double x2 = x * x;
                double x4 = x2 * x2;
                double x6 = x4 * x2;

                double tEx_k0rho = t_ * E_ * x / (k0 * rho_);
                double term1 = tEx_k0rho / 2;
                double term2 = tEx_k0rho * x2 / 24 * (-4 * psi2 + 9 * psi_ * (1 - t2) + 12 * t2);
                double term3 = tEx_k0rho * x4 * (8 * psi4 * (11 - 24 * t2) - 12 * psi3 * (21 - 71 * t2)
                    + 15 * psi2 * (15 - 98 * t2 + 15 * t4) + 180 * psi_ * (5 * t2 - 3 * t4) + 360 * t4) / 720;
                double term4 = tEx_k0rho * x6 / 40320 * (1385 - 3633 * t2 + 4095 * t4 + 1575 * t6);
                double phi = phi_ - term1 + term2 - term3 + term4;

                double xsecphi = x / Math.Cos(phi_);
                term1 = xsecphi;
                term2 = xsecphi * x2 / 6 * (psi_ + 2 * t2);
                term3 = xsecphi * x4 / 120 *
                  (-4 * psi3 * (1 - 6 * t2) + psi2 * (9 - 68 * t2) + 72 * psi_ * t2 + 24 * t4);
                term4 = xsecphi * x6 / 5040 * (61 + 662 * t2 + 1320 * t4 + 720 * t6);
                double lambda = lon0 + term1 - term2 + term3 - term4;

                return new Point3d(lambda, phi, point.Z);
            }
            public Point3d FromGeo(Point3d point)
            {
                double sin_phi = Math.Sin(point.Y);
                double cos_phi = Math.Cos(point.Y);
                double cos2_phi = cos_phi * cos_phi;
                double cos4_phi = cos2_phi * cos2_phi;
                double cos6_phi = cos4_phi * cos2_phi;

                double tmp = 1 - Ellipsoid.e2 * sin_phi * sin_phi;
                double rho = Ellipsoid.a * (1 - Ellipsoid.e2) / Math.Pow(tmp, 1.5);

                double nu = Ellipsoid.a / Math.Sqrt(tmp);
                double nu_sincos_phi = nu * sin_phi * cos_phi;

                double psi = nu / rho;
                double psi2 = psi * psi;
                double psi3 = psi2 * psi;
                double psi4 = psi2 * psi2;

                double t = Math.Tan(point.Y);
                double t2 = t * t;
                double t4 = t2 * t2;
                double t6 = t4 * t2;

                double omega = point.X - lon0;
                double omega2 = omega * omega;
                double omega4 = omega2 * omega2;
                double omega6 = omega4 * omega2;
                double omega8 = omega4 * omega4;

                double term1 = omega2 * nu_sincos_phi / 2;
                double term2 = omega4 * nu_sincos_phi * cos2_phi / 24 * (4 * psi2 + psi - t2);
                double term3 = omega6 * nu_sincos_phi * cos4_phi / 720
                    * (8 * psi4 * (11 - 24 * t2) - 28 * psi3 * (1 - 6 * t2)
                    + psi2 * (1 - 32 * t2) - 2 * psi * t2 + t4);
                double term4 = omega8 * nu_sincos_phi * cos6_phi / 40320 * (1385 - 3111 * t2 + 543 * t4 - t6);

                double N = N0 + k0 * (m(point.Y) - m0 + term1 + term2 + term3 + term4);

                term1 = omega2 * cos2_phi * (psi - t2) / 6;
                term2 = omega4 * cos4_phi / 120 * (4 * psi3 * (1 - 6 * t2) + psi2 * (1 + 8 * t2) - 2 * psi * t2 + t4);
                term3 = omega6 * cos6_phi / 5040 * (61 - 479 * t2 + 179 * t4 - t6);

                double E = E0 + k0 * nu * omega * cos_phi * (1 + term1 + term2 + term3);

                return new Point3d(E, N, point.Z);
            }
            public readonly Ellipsoids ellipsoid;
            private readonly Ellipsoid Ellipsoid;
            private readonly double lat0, lon0, E0, N0, k0, m0, n, G, A0, A2, A4, A6;
        }
        private class Transformation
        {
            public Transformation(Ellipsoids source, Ellipsoids target, Point3d t, Point3d r, double ds)
            {
                Source = ellipsoids[(int)source];
                Target = ellipsoids[(int)target];
                T = t;
                R = r;
                Ds = ds;
            }
            public Point3d Transform(Point3d point)
            {
                // geographic -> cartesian
                double sin_phi = Math.Sin(point.Y);
                double cos_phi = Math.Cos(point.Y);
                double nu1 = Source.a / Math.Sqrt(1 - Source.e2 * sin_phi * sin_phi);

                double X1 = (nu1 + point.Z) * cos_phi * Math.Cos(point.X);
                double Y1 = (nu1 + point.Z) * cos_phi * Math.Sin(point.X);
                double Z1 = (nu1 * (1 - Source.e2) + point.Z) * sin_phi;

                // transformation
                //double d = 1 + d_s*/1000000;
                double d = Ds;

                double X2 = T.X + (X1 + Y1 * R.Z - Z1 * R.Y) * d;
                double Y2 = T.Y + (-X1 * R.Z + Y1 + Z1 * R.X) * d;
                double Z2 = T.Z + (X1 * R.Y - Y1 * R.X + Z1) * d;

                // cartesian -> geographic
                //double e2 = target->e2();
                //double f = target->f();
                //double a = target->a();

                double p2 = X2 * X2 + Y2 * Y2;
                double p = Math.Sqrt(p2);
                double r = Math.Sqrt(p2 + Z2 * Z2);
                double mu = Math.Atan(Z2 * (1 - Target.f + Target.e2 * Target.a / r) / p);
                double sin_mu = Math.Sin(mu);
                double cos_mu = Math.Cos(mu);

                double lambda = Math.Atan(Y2 / X2);
                double phi = Math.Atan((Z2 * (1 - Target.f) + Target.e2 * Target.a * sin_mu * sin_mu * sin_mu) /
                                       ((1 - Target.f) * (p - Target.e2 * Target.a * cos_mu * cos_mu * cos_mu)));

                return new Point3d(lambda, phi, point.Z);
            }
            private readonly Ellipsoid Source;
            private readonly Ellipsoid Target;
            private readonly Point3d T;
            private readonly Point3d R;
            private readonly double Ds;
        }

        private static readonly Ellipsoid[] ellipsoids =
        {
            new Ellipsoid(6378137, 0.0033528105523340909524092138070416), // WGS-84
            new Ellipsoid(6378137, 0.003352810681225) // GRS-80
        };

        private static readonly Projection[] projections =
        {
            new Projection(Ellipsoids.GRS_80, 0.614434732254689, 0.553869654637742, 219529.584, 626907.3899999999, 1.0000067), // ITM
            new Projection(Ellipsoids.WGS_84, 0.575958653, 0, 500000, 0, 0.9996), // UTM
           // new Projection(Ellipsoids.WGS_84, 0.57595865315812876038481795360124, 0, 200000, -3500000, 0.99995), // CGRS
           // new Projection(Ellipsoids.WGS_84, 0, 0.72431163957764677442333166892277, 0, 2000000, 1) // EMB
        };

        private static readonly Transformation[,] transformations =
        {
            {null, new Transformation(Ellipsoids.WGS_84, Ellipsoids.GRS_80, new Point3d(-24.0024, -17.1032, -17.8444), new Point3d(-.0000016003, -.000008982, .0000080949), 1.0000054248)},
            {new Transformation(Ellipsoids.GRS_80, Ellipsoids.WGS_84, new Point3d(24.0024, 17.1032, 17.8444), new Point3d(.0000016003, .000008982, -.0000080949), 1/1.0000054248), null}
        };
    }
}
