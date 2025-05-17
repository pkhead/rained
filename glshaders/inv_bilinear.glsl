#ifndef INVBILINEAR_INC
#define INVBILINEAR_INC

// https://www.shadertoy.com/view/lsBSDm
// given a point p and a quad defined by four points {a,b,c,d}, return the bilinear
// coordinates of p in the quad. Will not be in the range [0..1]^2 if the point is
// outside the quad.
float cross2d( in vec2 a, in vec2 b ) { return a.x*b.y - a.y*b.x; }
vec2 inv_bilinear( in vec2 p, in vec2 a, in vec2 b, in vec2 c, in vec2 d )
{
    vec2 res = vec2(-1.0);

    vec2 e = b-a;
    vec2 f = d-a;
    vec2 g = a-b+c-d;
    vec2 h = p-a;
        
    float k2 = cross2d( g, f );
    float k1 = cross2d( e, f ) + cross2d( h, g );
    float k0 = cross2d( h, e );
    
    // if edges are parallel, this is a linear equation
    if( abs(k2)<0.001 )
    {
        res = vec2( (h.x*k1+f.x*k0)/(e.x*k1-g.x*k0), -k0/k1 );
    }
    // otherwise, it's a quadratic
	else
    {
        float w = k1*k1 - 4.0*k0*k2;
        if( w<0.0 ) return vec2(-1.0);
        w = sqrt( w );

        float ik2 = 0.5/k2;
        float v = (-k1 - w)*ik2;
        float u = (h.x - f.x*v)/(e.x + g.x*v);
        
        if( u<0.0 || u>1.0 || v<0.0 || v>1.0 )
        {
           v = (-k1 + w)*ik2;
           u = (h.x - f.x*v)/(e.x + g.x*v);
        }
        res = vec2( u, v );
    }
    
    return res;
}

#endif