with import <nixpkgs> {};

{
  fsiEnv =  stdenv.mkDerivation {
    name = "testEnv";
    buildInputs = [ stdenv curl zlib openssl ];
    libpath="${curl.out}/lib:${openssl.out}/lib:${zlib.out}/lib";
    shellHook = ''
      export DISCO_NODE_ID=`uuidgen`
      export LD_LIBRARY_PATH="$libpath":$LD_LIBRARY_PATH
      mono Disco.Tests.exe
      exit $?
    '';
  };
}
