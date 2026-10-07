class ClaseAbstracta:
    """
    Representa la Clase Abstracta ClaseAbstracta
    """

    def __init__(self):
        self.CampoPublico = None  # type: Any (Public)
        self.__CampoPrivado = None  # type: Any (Private)
        self._CampoProtegido = None  # type: Any (Protected)
        self._CampoPaquete = None  # type: Any (Package-Private)

    def MetodoPublico(self, string, integer):
        """
        Retorna: int
        """
        raise NotImplementedError("El método MetodoPublico no está implementado.")

    def __MetodoPrivado(self, double, float):
        """
        Retorna: double
        """
        raise NotImplementedError("El método __MetodoPrivado no está implementado.")

    def _MetodoProtegido(self, boolean):
        """
        Retorna: boolean
        """
        raise NotImplementedError("El método _MetodoProtegido no está implementado.")

    def _MetodoPaquete(self):
        """
        Retorna: void
        """
        raise NotImplementedError("El método _MetodoPaquete no está implementado.")

