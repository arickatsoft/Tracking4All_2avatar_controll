import com.fitfight.camera.FitFightCameraPixels;

public final class CameraPixelsTest {
    private static int checks;
    private static void check(boolean value,String label) { if(!value) throw new AssertionError(label);checks++; }
    public static void main(String[] args) {
        int[][] expected={{0,1,2,3,4,5},{4,2,0,5,3,1},{5,4,3,2,1,0},{1,3,5,0,2,4}};
        for(int turn=0;turn<4;turn++) {
            int w=turn%2==0?2:3,h=turn%2==0?3:2;
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
                int sx=FitFightCameraPixels.sourceX(x,y,2,3,turn*90),sy=FitFightCameraPixels.sourceY(x,y,2,3,turn*90);
                check(sx>=0 && sx<2 && sy>=0 && sy<3,"Sensor bounds");
                check(sy*2+sx==expected[turn][y*w+x],"Upright orientation "+turn*90);
            }
        }
        check(FitFightCameraPixels.rgba(16,128,128)==0xff000000,"Black RGBA channel order");
        check(FitFightCameraPixels.rgba(235,128,128)==0xffffffff,"White RGBA channel order");
        int red=FitFightCameraPixels.rgba(81,90,240);
        check((red&255)>250 && ((red>>8)&255)<3 && ((red>>16)&255)<3,"Red packed as R,G,B,A bytes");
        int blue=FitFightCameraPixels.rgba(41,240,110);
        check(((blue>>16)&255)>250 && (blue&255)<3,"Blue packed channel order");
        System.out.println("PASS "+checks+" camera rotation/YUV checks; device lifecycle and throughput require a phone.");
    }
}
