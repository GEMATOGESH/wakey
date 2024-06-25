import os
from dotenv import dotenv_values
from zipfile import ZipFile

def main():
    env = dotenv_values(".env")
    
    path = env["project_name"] + "\\bin\\Debug\\net7.0"
    files = env["files"].split(",")
    mod_folder = env["mod_folder"]
    
    print(path)
    print(str(files))
    print(mod_folder)
    
    with ZipFile(mod_folder + "\\" + env["project_name"] + ".zip", 'w') as zip:
        for filename in files:
            a = filename.find(".")
            if a != -1:
                zip.write(path + "\\" + filename, filename)
            else:
                for subdir, dirs, files in os.walk(path + "\\" + filename):
                    for file in files:
                        srcpath = os.path.join(subdir, file)
                        dstpath_in_zip = os.path.relpath(srcpath, start=path)
                        with open(srcpath, 'rb') as infile:
                            zip.writestr(dstpath_in_zip, infile.read())

main()
