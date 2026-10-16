file(REMOVE_RECURSE
  "libyuv.dylib"
  "libyuv.pdb"
)

# Per-language clean rules from dependency scanning.
foreach(lang CXX)
  include(CMakeFiles/yuv_shared.dir/cmake_clean_${lang}.cmake OPTIONAL)
endforeach()
