#ifndef _CLASEABSTRACTA_H_
#define _CLASEABSTRACTA_H_

#include <string>
#include <stdexcept>

class ClaseAbstracta {
public:
    void* CampoPublico; // TODO: Especificar tipo de dato
    virtual ~ClaseAbstracta() = default;
    virtual int MetodoPublico(void* string, void* integer) = 0;
protected:
    void* CampoProtegido; // TODO: Especificar tipo de dato
    void* CampoPaquete; // TODO: Especificar tipo de dato
    virtual bool MetodoProtegido(void* boolean) = 0;
    virtual void MetodoPaquete() = 0;
private:
    void* CampoPrivado; // TODO: Especificar tipo de dato
    virtual double MetodoPrivado(void* double, void* float) = 0;
};

#endif
