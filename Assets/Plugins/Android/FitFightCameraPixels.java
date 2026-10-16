package com.fitfight.camera;

/** Camera sensor-to-upright mapping and limited-range BT.601 YUV conversion. */
public final class FitFightCameraPixels {
    private FitFightCameraPixels() { }
    public static int sourceX(int x,int y,int width,int height,int rotation) {
        if(rotation==90) return y;
        if(rotation==180) return width-1-x;
        if(rotation==270) return width-1-y;
        return x;
    }
    public static int sourceY(int x,int y,int width,int height,int rotation) {
        if(rotation==90) return height-1-x;
        if(rotation==180) return height-1-y;
        if(rotation==270) return x;
        return y;
    }
    public static int rgba(int y,int u,int v) {
        int c=Math.max(0,y-16)*298,uu=u-128,vv=v-128;
        int r=clamp((c+409*vv+128)>>8),g=clamp((c-100*uu-208*vv+128)>>8),b=clamp((c+516*uu+128)>>8);
        return r|(g<<8)|(b<<16)|0xff000000;
    }
    private static int clamp(int v) { return Math.max(0,Math.min(255,v)); }
}
